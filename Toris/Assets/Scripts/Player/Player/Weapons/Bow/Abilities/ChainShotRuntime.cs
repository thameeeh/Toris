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
    private const float MinimumProjectileSpeed = 0.1f;
    private const float MinimumProjectileLifetime = 0.05f;
    private const float MinimumLockedImpactDelay = 0.01f;
    private const float LockedImpactLifetimePadding = 0.05f;

    private sealed class DamageableTarget
    {
        public Collider2D collider;
        public IDamageable damageable;
        public Component component;
        public int instanceId;
    }

    private sealed class PendingBounce
    {
        public Vector2 startPoint;
        public float executeAtTime;
        public int damageIndex;
    }

    private sealed class PendingImpact
    {
        public DamageableTarget target;
        public Vector2 startPoint;
        public float executeAtTime;
        public float damage;
        public int nextDamageIndex;
        public ArrowProjectile visualProjectile;
    }

    private sealed class ChainShotSession
    {
        public PlayerBowController bow;
        public ChainShotCastSettings settings;
        public readonly HashSet<int> visitedTargets = new HashSet<int>();
        public readonly List<PendingBounce> pendingBounces = new List<PendingBounce>();
        public readonly List<PendingImpact> pendingImpacts = new List<PendingImpact>();
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
            ProcessPendingImpacts(session, currentTime);

            if (session.activeProjectileCount <= 0
                && session.pendingBounces.Count == 0
                && session.pendingImpacts.Count == 0)
            {
                _activeSessions.RemoveAt(sessionIndex);
            }
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

    private void ProcessPendingImpacts(ChainShotSession session, float currentTime)
    {
        for (int impactIndex = session.pendingImpacts.Count - 1; impactIndex >= 0; impactIndex--)
        {
            PendingImpact pendingImpact = session.pendingImpacts[impactIndex];
            if (currentTime < pendingImpact.executeAtTime)
                continue;

            session.pendingImpacts.RemoveAt(impactIndex);
            ResolveLockedImpact(session, pendingImpact);
        }
    }

    private void SpawnBounceProjectile(ChainShotSession session, PendingBounce pendingBounce)
    {
        if (session.bow == null)
            return;

        if (session.settings.damageMultipliers == null || pendingBounce.damageIndex >= session.settings.damageMultipliers.Length)
            return;

        float safeSearchRadius = Mathf.Max(0.05f, session.settings.chainSearchRadius);
        if (!TryFindNearestTarget(pendingBounce.startPoint, safeSearchRadius, session.visitedTargets, out DamageableTarget nextTarget))
        {
            Log(session.bow,
                $"Bounce stopped. No valid target found from point={FormatVector(pendingBounce.startPoint)} within radius={safeSearchRadius:F2}");
            return;
        }

        Vector2 targetPoint = GetTargetPoint(nextTarget, pendingBounce.startPoint);
        Vector2 direction = targetPoint - pendingBounce.startPoint;
        if (direction.sqrMagnitude < MinDirectionSqrMagnitude && nextTarget.component != null)
            direction = (Vector2)nextTarget.component.transform.position - pendingBounce.startPoint;

        float travelDistance = direction.magnitude;
        Vector2 bounceDirection = direction.sqrMagnitude >= MinDirectionSqrMagnitude ? direction.normalized : Vector2.right;
        float safeProjectileSpeed = Mathf.Max(MinimumProjectileSpeed, session.settings.chainProjectileSpeed);
        float travelTime = Mathf.Max(MinimumLockedImpactDelay, travelDistance / safeProjectileSpeed);
        float safeProjectileLifetime = Mathf.Max(MinimumProjectileLifetime, session.settings.chainProjectileLifetime);
        float visualLifetime = Mathf.Max(
            safeProjectileLifetime,
            travelTime + LockedImpactLifetimePadding);
        float damageMultiplier = session.settings.damageMultipliers[pendingBounce.damageIndex];
        float resolvedDamage = session.bow.ResolveOutgoingDamage(session.settings.baseDamage * damageMultiplier);

        Vector3 spawnPoint = pendingBounce.startPoint + (bounceDirection * ChainShotSpawnOffset);
        ArrowProjectile visualProjectile = SpawnVisualBounceArrow(session, spawnPoint, bounceDirection, safeProjectileSpeed, visualLifetime);

        session.pendingImpacts.Add(new PendingImpact
        {
            target = nextTarget,
            startPoint = pendingBounce.startPoint,
            executeAtTime = Time.time + travelTime,
            damage = resolvedDamage,
            nextDamageIndex = pendingBounce.damageIndex + 1,
            visualProjectile = visualProjectile
        });

        Log(session.bow,
            $"Bounce locked. index={pendingBounce.damageIndex} from={FormatVector(pendingBounce.startPoint)} to={FormatVector(targetPoint)} damage={resolvedDamage:F2} speed={safeProjectileSpeed:F2} impactIn={travelTime:F2}s");
    }

    private bool TryFindNearestTarget(Vector2 origin, float searchRadius, HashSet<int> visitedTargets, out DamageableTarget nearestTarget)
    {
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            origin,
            searchRadius,
            _overlapResults,
            BowAbilityTargetingUtility.GetEnemyHurtBoxMask());

        nearestTarget = null;
        float nearestDistanceSqr = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D overlapCollider = _overlapResults[i];
            if (!BowAbilityTargetingUtility.IsEnemyHurtBoxCollider(overlapCollider))
                continue;

            IDamageable damageableTarget = overlapCollider.GetComponentInParent<IDamageable>();
            if (damageableTarget == null)
                continue;

            if (damageableTarget.CurrentHealth <= 0f)
                continue;

            Component damageableComponent = damageableTarget as Component;
            if (damageableComponent == null)
                continue;

            int targetId = damageableComponent.GetInstanceID();
            if (visitedTargets.Contains(targetId))
                continue;

            Vector2 candidatePoint = overlapCollider.ClosestPoint(origin);
            float distanceSqr = (candidatePoint - origin).sqrMagnitude;
            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestTarget = new DamageableTarget
                {
                    collider = overlapCollider,
                    damageable = damageableTarget,
                    component = damageableComponent,
                    instanceId = targetId
                };
            }
        }

        return nearestTarget != null;
    }

    private void ResolveLockedImpact(ChainShotSession session, PendingImpact pendingImpact)
    {
        DespawnVisualProjectile(pendingImpact);

        DamageableTarget target = pendingImpact.target;
        if (target == null || target.damageable == null)
            return;

        if (target.component == null)
            return;

        if (!target.component.gameObject.activeInHierarchy)
            return;

        if (target.instanceId != 0 && session.visitedTargets.Contains(target.instanceId))
            return;

        if (target.damageable.CurrentHealth <= 0f)
            return;

        Vector2 hitPoint = GetTargetPoint(target, pendingImpact.startPoint);
        if (target.damageable is IDirectHitDamageable directHitDamageable)
            directHitDamageable.ApplyDirectHit(pendingImpact.damage, hitPoint);
        else
            target.damageable.Damage(pendingImpact.damage);

        if (session.settings.playImpactEffect && session.bow != null)
            session.bow.PlayDefaultArrowHitEffect(hitPoint);

        if (target.instanceId != 0)
            session.visitedTargets.Add(target.instanceId);

        Log(session.bow,
            $"Locked hit confirmed. target={target.component.name} hitPoint={FormatVector(hitPoint)} nextDamageIndex={pendingImpact.nextDamageIndex}");

        if (session.settings.damageMultipliers == null || pendingImpact.nextDamageIndex >= session.settings.damageMultipliers.Length)
            return;

        session.pendingBounces.Add(new PendingBounce
        {
            startPoint = hitPoint,
            executeAtTime = Time.time + Mathf.Max(0f, session.settings.chainJumpDelay),
            damageIndex = pendingImpact.nextDamageIndex
        });
    }

    private static Vector2 GetTargetPoint(DamageableTarget target, Vector2 origin)
    {
        if (target.collider != null && target.collider.enabled)
            return target.collider.ClosestPoint(origin);

        if (target.component != null)
            return target.component.transform.position;

        return origin;
    }

    private static void DespawnVisualProjectile(PendingImpact pendingImpact)
    {
        if (pendingImpact.visualProjectile == null)
            return;

        if (!pendingImpact.visualProjectile.gameObject.activeInHierarchy)
            return;

        pendingImpact.visualProjectile.Despawn();
    }

    private static ArrowProjectile SpawnVisualBounceArrow(
        ChainShotSession session,
        Vector3 spawnPoint,
        Vector2 direction,
        float speed,
        float lifetimeSeconds)
    {
        BowSO bowConfig = session.bow != null ? session.bow.BowConfig : null;
        if (bowConfig == null || bowConfig.arrowPrefab == null)
            return null;

        Projectile projectilePrefab = bowConfig.arrowPrefab;
        GameplayPoolManager manager = GameplayPoolManager.Instance;
        Projectile projectile = manager != null
            ? manager.SpawnProjectile(projectilePrefab, spawnPoint, Quaternion.identity)
            : Object.Instantiate(projectilePrefab, spawnPoint, Quaternion.identity);

        ArrowProjectile arrow = projectile as ArrowProjectile;
        if (arrow == null)
        {
            if (projectile != null)
                projectile.Despawn();

            return null;
        }

        arrow.InitializeVisualOnly(direction, speed, lifetimeSeconds);
        return arrow;
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
