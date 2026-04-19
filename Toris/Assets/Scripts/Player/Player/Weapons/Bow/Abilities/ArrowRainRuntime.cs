using System.Collections.Generic;
using UnityEngine;

public struct ArrowRainCastSettings
{
    public Vector2 castOrigin;
    public float maxTargetRange;
    public float duration;
    public float firstBurstDelay;
    public float burstInterval;
    public int strikesPerBurst;
    public float zoneRadius;
    public float impactRadius;
    public bool guaranteeCenterStrike;
    public float damagePerStrike;
    public GameObject arrowRainVisualPrefab;
    public bool spawnVisualArrows;
    public float visualArrowHeight;
    public float visualArrowSpeed;
    public bool playImpactEffect;
}

[System.Serializable]
public sealed class ArrowRainRuntime : PlayerAbilityRuntime
{
    private const int MaxOverlapResults = 16;
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const float VisualLifetimePadding = 0.05f;

    private struct PendingStrike
    {
        public Vector2 point;
        public float damage;
        public float resolveAtTime;
    }

    private bool _isActive;
    private float _activationStartTime;
    private float _activationEndTime;
    private float _nextBurstTime;
    private float _nextBurstElapsed;
    private float _safeDuration;
    private float _safeBurstInterval;
    private bool _firstBurstPending;
    private bool _hasMoreBursts;
    private Vector2 _center;
    private ArrowRainCastSettings _settings;
    private readonly List<PendingStrike> _pendingStrikes = new List<PendingStrike>();
    private readonly Collider2D[] _overlapResults = new Collider2D[MaxOverlapResults];
    private readonly HashSet<IDamageable> _damagedTargets = new HashSet<IDamageable>();

    public bool IsActive => _isActive;

    public void Activate(PlayerAbilityContext context, Vector2 center, ArrowRainCastSettings settings)
    {
        _settings = settings;
        _center = ClampPointToRange(center, settings);
        _pendingStrikes.Clear();
        _isActive = true;
        _activationStartTime = Time.time;
        _safeDuration = Mathf.Max(0.01f, settings.duration);
        _activationEndTime = _activationStartTime + _safeDuration;
        _safeBurstInterval = Mathf.Max(0.01f, settings.burstInterval);
        _nextBurstElapsed = Mathf.Clamp(settings.firstBurstDelay, 0f, _safeDuration);
        _nextBurstTime = _activationStartTime + _nextBurstElapsed;
        _firstBurstPending = true;
        _hasMoreBursts = true;

        Log(context.bow,
            $"Cast accepted. castOrigin={FormatVector(settings.castOrigin)} requestedCenter={FormatVector(center)} clampedCenter={FormatVector(_center)} maxTargetRange={settings.maxTargetRange:F2} zoneRadius={settings.zoneRadius:F2}");
        Log(context.bow,
            $"Started. center={FormatVector(_center)} zoneRadius={settings.zoneRadius:F2} duration={_safeDuration:F2} burstInterval={_safeBurstInterval:F2} strikesPerBurst={settings.strikesPerBurst}");
    }

    public void Tick(PlayerAbilityContext context)
    {
        if (!_isActive)
            return;

        float currentTime = Time.time;
        while (_hasMoreBursts && currentTime >= _nextBurstTime)
        {
            ResolveBurst(context, _firstBurstPending);
            _firstBurstPending = false;

            if (_nextBurstElapsed + _safeBurstInterval > _safeDuration)
            {
                _hasMoreBursts = false;
            }
            else
            {
                _nextBurstElapsed += _safeBurstInterval;
                _nextBurstTime = _activationStartTime + _nextBurstElapsed;
            }
        }

        for (int strikeIndex = _pendingStrikes.Count - 1; strikeIndex >= 0; strikeIndex--)
        {
            PendingStrike pendingStrike = _pendingStrikes[strikeIndex];
            if (currentTime < pendingStrike.resolveAtTime)
                continue;

            _pendingStrikes.RemoveAt(strikeIndex);
            ResolveStrike(context, pendingStrike);
        }

        if (!_hasMoreBursts && _pendingStrikes.Count == 0 && currentTime >= _activationEndTime)
            _isActive = false;
    }

    private void ResolveBurst(PlayerAbilityContext context, bool isFirstBurst)
    {
        int strikeCount = Mathf.Max(1, _settings.strikesPerBurst);
        float finalDamage = context.bow != null
            ? context.bow.ResolveOutgoingDamage(_settings.damagePerStrike)
            : Mathf.Max(0f, _settings.damagePerStrike);
        float zoneRadius = Mathf.Max(0f, _settings.zoneRadius);

        Log(context.bow,
            $"Burst. center={FormatVector(_center)} firstBurst={isFirstBurst} strikeCount={strikeCount} damage={finalDamage:F2}");

        int remainingStrikes = strikeCount;
        if (isFirstBurst && _settings.guaranteeCenterStrike)
        {
            QueueStrike(context.bow, _center, finalDamage);
            remainingStrikes--;
        }

        if (remainingStrikes <= 0)
            return;

        if (isFirstBurst)
        {
            float ringRadius = zoneRadius * 0.65f;
            float angleStep = 360f / remainingStrikes;
            for (int i = 0; i < remainingStrikes; i++)
            {
                float angle = i * angleStep;
                Vector2 offset = Quaternion.Euler(0f, 0f, angle) * Vector2.right * ringRadius;
                QueueStrike(context.bow, _center + offset, finalDamage);
            }
            return;
        }

        for (int i = 0; i < remainingStrikes; i++)
        {
            Vector2 strikePoint = _center + (Random.insideUnitCircle * zoneRadius);
            QueueStrike(context.bow, strikePoint, finalDamage);
        }
    }

    private void QueueStrike(PlayerBowController playerBow, Vector2 strikePoint, float damage)
    {
        Vector2 clampedStrikePoint = ClampPointToRange(strikePoint, _settings);
        float strikeDistance = Vector2.Distance(_settings.castOrigin, clampedStrikePoint);
        Log(playerBow,
            $"Strike queued. requested={FormatVector(strikePoint)} clamped={FormatVector(clampedStrikePoint)} distanceFromCastOrigin={strikeDistance:F2} maxTargetRange={_settings.maxTargetRange:F2}");

        float impactDelay = SpawnVisual(clampedStrikePoint);
        _pendingStrikes.Add(new PendingStrike
        {
            point = clampedStrikePoint,
            damage = damage,
            resolveAtTime = Time.time + impactDelay
        });
    }

    private void ResolveStrike(PlayerAbilityContext context, PendingStrike pendingStrike)
    {
        if (_settings.playImpactEffect)
            PlayImpactEffect(context.bow, pendingStrike.point);

        float safeImpactRadius = Mathf.Max(0.05f, _settings.impactRadius);
        int hitCount = Physics2D.OverlapCircleNonAlloc(
            pendingStrike.point,
            safeImpactRadius,
            _overlapResults,
            BowAbilityTargetingUtility.GetEnemyHurtBoxMask());

        Log(context.bow,
            $"Strike resolved. point={FormatVector(pendingStrike.point)} distanceFromCastOrigin={Vector2.Distance(_settings.castOrigin, pendingStrike.point):F2} impactRadius={safeImpactRadius:F2} hitCount={hitCount}");

        _damagedTargets.Clear();
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D overlapCollider = _overlapResults[i];
            if (!BowAbilityTargetingUtility.IsEnemyHurtBoxCollider(overlapCollider))
                continue;

            IDamageable damageable = overlapCollider.GetComponentInParent<IDamageable>();
            if (damageable == null || !_damagedTargets.Add(damageable))
                continue;

            Vector2 closestPoint = overlapCollider.ClosestPoint(pendingStrike.point);
            Component damageableComponent = damageable as Component;
            string targetName = damageableComponent != null ? damageableComponent.name : damageable.GetType().Name;
            Vector2 targetPosition = damageableComponent != null
                ? (Vector2)damageableComponent.transform.position
                : closestPoint;

            Log(context.bow,
                $"Damage hit. target={targetName} collider={overlapCollider.name} strikePoint={FormatVector(pendingStrike.point)} closestPoint={FormatVector(closestPoint)} closestDistanceFromStrike={Vector2.Distance(pendingStrike.point, closestPoint):F2} targetPosition={FormatVector(targetPosition)} targetDistanceFromCastOrigin={Vector2.Distance(_settings.castOrigin, targetPosition):F2}");
            damageable.Damage(pendingStrike.damage);
        }
    }

    private float SpawnVisual(Vector2 strikePoint)
    {
        if (!_settings.spawnVisualArrows || _settings.arrowRainVisualPrefab == null)
            return 0f;

        float safeHeight = Mathf.Max(0f, _settings.visualArrowHeight);
        float safeSpeed = Mathf.Max(0.1f, _settings.visualArrowSpeed);
        float flightTime = safeHeight / safeSpeed;
        Vector3 spawnPoint = strikePoint + (Vector2.up * safeHeight);
        GameObject visualObject = Object.Instantiate(_settings.arrowRainVisualPrefab, spawnPoint, Quaternion.identity);
        ArrowRainVisual visual = visualObject.GetComponent<ArrowRainVisual>();

        if (visual == null)
        {
            Object.Destroy(visualObject);
            return 0f;
        }

        visual.Initialize(
            spawnPoint,
            strikePoint,
            flightTime + VisualLifetimePadding);

        return flightTime;
    }

    private static Vector2 ClampPointToRange(Vector2 point, ArrowRainCastSettings settings)
    {
        if (settings.maxTargetRange <= 0f)
            return point;

        Vector2 offset = point - settings.castOrigin;
        float maxRangeSqr = settings.maxTargetRange * settings.maxTargetRange;
        if (offset.sqrMagnitude <= maxRangeSqr)
            return point;

        if (offset.sqrMagnitude <= MinDirectionSqrMagnitude)
            return settings.castOrigin;

        return settings.castOrigin + (offset.normalized * settings.maxTargetRange);
    }

    private static void PlayImpactEffect(PlayerBowController playerBow, Vector2 strikePoint)
    {
        if (playerBow == null)
            return;

        playerBow.PlayDefaultArrowHitEffect(strikePoint);
    }

    private static void Log(Object context, string message)
    {
        PlayerShootDebug.Log(context, "ArrowRain", message);
    }

    private static string FormatVector(Vector2 value)
    {
        return $"({value.x:F2}, {value.y:F2})";
    }
}
