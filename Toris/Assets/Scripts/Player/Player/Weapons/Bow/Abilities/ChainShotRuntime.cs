using System.Collections.Generic;
using UnityEngine;

public struct ChainShotCastSettings
{
    public float baseDamage;
    public float[] damageMultipliers;
    public float chainSearchRadius;
    public float chainProjectileSpeed;
    public float chainProjectileLifetime;
    public float chainJumpDelay;
    public bool playImpactEffect;
}

[System.Serializable]
public sealed class ChainShotRuntime : PlayerAbilityRuntime
{
    private const int MaxOverlapResults = 16;
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const float ChainShotSpawnOffset = 0.05f;

    private sealed class PendingBounce
    {
        public Vector2 startPoint;
        public Collider2D previousHitCollider;
        public float executeAtTime;
        public int damageIndex;
    }

    private sealed class ChainShotSession
    {
        public PlayerBowController bow;
        public ChainShotCastSettings settings;
        public readonly HashSet<int> visitedTargets = new HashSet<int>();
        public readonly List<PendingBounce> pendingBounces = new List<PendingBounce>();
        public int activeProjectileCount;
    }

    private readonly List<ChainShotSession> _activeSessions = new List<ChainShotSession>();
    private readonly Collider2D[] _overlapResults = new Collider2D[MaxOverlapResults];

    public void StartCast(PlayerAbilityContext context, BowSO.ShotStats baseShotStats, ChainShotCastSettings settings, bool playReleaseAnimation)
    {
        PlayerBowController playerBow = context.bow;
        if (playerBow == null || settings.damageMultipliers == null || settings.damageMultipliers.Length == 0)
            return;

        BowSO.ShotStats firstShotStats = baseShotStats;
        firstShotStats.damage = playerBow.ResolveOutgoingDamage(settings.baseDamage * settings.damageMultipliers[0]);

        Vector2 baseDirection = playerBow.CurrentAimDirection;
        if (baseDirection.sqrMagnitude < MinDirectionSqrMagnitude)
            baseDirection = Vector2.right;

        Log(playerBow,
            $"Cast started. dir={FormatVector(baseDirection)} firstDamage={firstShotStats.damage:F2} firstSpeed={firstShotStats.speed:F2} chainTargets={settings.damageMultipliers.Length} searchRadius={settings.chainSearchRadius:F2}");

        ChainShotSession session = new ChainShotSession
        {
            bow = playerBow,
            settings = settings
        };

        ArrowProjectile projectile = playerBow.SpawnArrowFromAim(firstShotStats, playReleaseAnimation, true);
        if (projectile == null)
            return;

        _activeSessions.Add(session);
        ConfigureProjectile(session, projectile, 1);
    }

    public void Tick(PlayerAbilityContext context)
    {
        if (_activeSessions.Count == 0)
            return;

        float currentTime = Time.time;
        for (int sessionIndex = _activeSessions.Count - 1; sessionIndex >= 0; sessionIndex--)
        {
            ChainShotSession session = _activeSessions[sessionIndex];
            ProcessPendingBounces(session, currentTime);

            if (session.activeProjectileCount <= 0 && session.pendingBounces.Count == 0)
                _activeSessions.RemoveAt(sessionIndex);
        }
    }

    private void ProcessPendingBounces(ChainShotSession session, float currentTime)
    {
        for (int bounceIndex = session.pendingBounces.Count - 1; bounceIndex >= 0; bounceIndex--)
        {
            PendingBounce pendingBounce = session.pendingBounces[bounceIndex];
            if (currentTime < pendingBounce.executeAtTime)
                continue;

            session.pendingBounces.RemoveAt(bounceIndex);
            SpawnBounceProjectile(session, pendingBounce);
        }
    }

    private void SpawnBounceProjectile(ChainShotSession session, PendingBounce pendingBounce)
    {
        if (session.settings.damageMultipliers == null || pendingBounce.damageIndex >= session.settings.damageMultipliers.Length)
            return;

        float safeSearchRadius = Mathf.Max(0.05f, session.settings.chainSearchRadius);
        Collider2D nextTargetCollider = FindNearestTarget(pendingBounce.startPoint, safeSearchRadius, session.visitedTargets);
        if (nextTargetCollider == null)
        {
            Log(session.bow,
                $"Bounce stopped. No valid target found from point={FormatVector(pendingBounce.startPoint)} within radius={safeSearchRadius:F2}");
            return;
        }

        Vector2 targetPoint = nextTargetCollider.ClosestPoint(pendingBounce.startPoint);
        Vector2 direction = targetPoint - pendingBounce.startPoint;
        if (direction.sqrMagnitude < MinDirectionSqrMagnitude)
            return;

        float safeProjectileSpeed = Mathf.Max(0.1f, session.settings.chainProjectileSpeed);
        float safeProjectileLifetime = Mathf.Max(0.05f, session.settings.chainProjectileLifetime);
        float damageMultiplier = session.settings.damageMultipliers[pendingBounce.damageIndex];

        BowSO.ShotStats bounceShotStats = new BowSO.ShotStats
        {
            power = 1f,
            speed = safeProjectileSpeed,
            damage = session.bow.ResolveOutgoingDamage(session.settings.baseDamage * damageMultiplier),
            spreadDeg = 0f
        };

        float escapeDistance = ChainShotSpawnOffset;
        if (pendingBounce.previousHitCollider != null)
            escapeDistance += pendingBounce.previousHitCollider.bounds.extents.magnitude;

        Vector2 bounceDirection = direction.normalized;
        Vector3 spawnPoint = pendingBounce.startPoint + (bounceDirection * escapeDistance);
        ArrowProjectile projectile = session.bow.SpawnArrowFromWorld(
            bounceShotStats,
            bounceDirection,
            spawnPoint,
            false,
            "chain",
            safeProjectileLifetime);

        if (projectile == null)
            return;

        Log(session.bow,
            $"Bounce spawned. index={pendingBounce.damageIndex} from={FormatVector(pendingBounce.startPoint)} to={FormatVector(targetPoint)} damage={bounceShotStats.damage:F2} speed={bounceShotStats.speed:F2}");

        ConfigureProjectile(session, projectile, pendingBounce.damageIndex + 1);
    }

    private Collider2D FindNearestTarget(Vector2 origin, float searchRadius, HashSet<int> visitedTargets)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            origin,
            searchRadius,
            _overlapResults,
            BowAbilityTargetingUtility.GetEnemyHurtBoxMask());

        Collider2D nearestCollider = null;
        float nearestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D overlapCollider = _overlapResults[i];
            if (!BowAbilityTargetingUtility.IsEnemyHurtBoxCollider(overlapCollider))
                continue;

            IDamageable damageableTarget = overlapCollider.GetComponentInParent<IDamageable>();
            if (damageableTarget == null)
                continue;

            Component damageableComponent = damageableTarget as Component;
            if (damageableComponent != null && visitedTargets.Contains(damageableComponent.GetInstanceID()))
                continue;

            Vector2 candidatePoint = overlapCollider.ClosestPoint(origin);
            float distanceSqr = (candidatePoint - origin).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestCollider = overlapCollider;
            }
        }

        return nearestCollider;
    }

    private void ConfigureProjectile(ChainShotSession session, ArrowProjectile projectile, int nextDamageIndex)
    {
        session.activeProjectileCount++;

        int enemyHurtBoxLayer = BowAbilityTargetingUtility.GetEnemyHurtBoxLayer();
        if (enemyHurtBoxLayer >= 0)
            projectile.SetDamageLayerMask(1 << enemyHurtBoxLayer);

        projectile.SetPlayHitEffect(session.settings.playImpactEffect);
        projectile.SetDamageTargetPredicate(targetCollider =>
        {
            if (!BowAbilityTargetingUtility.IsEnemyHurtBoxCollider(targetCollider))
                return false;

            IDamageable damageableTarget = targetCollider.GetComponentInParent<IDamageable>();
            if (damageableTarget == null)
                return false;

            Component damageableComponent = damageableTarget as Component;
            return damageableComponent == null || !session.visitedTargets.Contains(damageableComponent.GetInstanceID());
        });

        System.Action<ArrowProjectile, Collider2D, IDamageable, Vector2> handleHit = null;
        System.Action<ArrowProjectile> handleProjectileDespawned = null;

        handleHit = (hitProjectile, hitCollider, damageableTarget, hitPoint) =>
        {
            hitProjectile.DamageApplied -= handleHit;

            if (!BowAbilityTargetingUtility.IsEnemyHurtBoxCollider(hitCollider))
                return;

            Component hitComponent = damageableTarget as Component;
            if (hitComponent != null)
                session.visitedTargets.Add(hitComponent.GetInstanceID());

            Log(session.bow,
                $"Hit confirmed. target={(hitComponent != null ? hitComponent.name : damageableTarget.GetType().Name)} hitPoint={FormatVector(hitPoint)} nextDamageIndex={nextDamageIndex}");

            if (session.settings.damageMultipliers == null || nextDamageIndex >= session.settings.damageMultipliers.Length)
                return;

            session.pendingBounces.Add(new PendingBounce
            {
                startPoint = hitPoint,
                previousHitCollider = hitCollider,
                executeAtTime = Time.time + Mathf.Max(0f, session.settings.chainJumpDelay),
                damageIndex = nextDamageIndex
            });
        };

        handleProjectileDespawned = despawnedProjectile =>
        {
            despawnedProjectile.DamageApplied -= handleHit;
            despawnedProjectile.ProjectileDespawned -= handleProjectileDespawned;
            session.activeProjectileCount = Mathf.Max(0, session.activeProjectileCount - 1);
        };

        projectile.DamageApplied += handleHit;
        projectile.ProjectileDespawned += handleProjectileDespawned;
    }

    private static void Log(Object context, string message)
    {
        PlayerShootDebug.Log(context, "ChainShot", message);
    }

    private static string FormatVector(Vector2 value)
    {
        return $"({value.x:F2}, {value.y:F2})";
    }
}
