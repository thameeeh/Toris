using System.Collections.Generic;
using UnityEngine;

public struct ArrowRainCastSettings
{
    public Vector2 castOrigin;
    public float maxTargetRange;
    public float duration;
    public float initialStrikeDelay;
    public float strikeInterval;
    public float zoneRadius;
    public float impactRadius;
    public bool guaranteeCenterStrike;
    public float damagePerStrike;
    public GameObject arrowRainVisualPrefab;
    public bool spawnVisualArrows;
    public float visualArrowHeight;
    public float visualArrowSpeed;
    public GameObject impactShadowPrefab;
    public bool spawnImpactShadow;
    public float impactShadowStartScaleMultiplier;
    public float impactShadowEndScaleMultiplier;
    public float impactShadowStartAlpha;
    public float impactShadowEndAlpha;
    public bool playImpactEffect;
}

[System.Serializable]
public sealed class ArrowRainRuntime : PlayerAbilityRuntime
{
    private const int MaxOverlapResults = 16;
    private const float MinDirectionSqrMagnitude = 0.0001f;
    private const float MinContinuousStrikeSpacing = 0.01f;
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
    private float _nextStrikeTime;
    private float _nextStrikeElapsed;
    private float _safeDuration;
    private float _safeStrikeInterval;
    private int _emittedStrikeCount;
    private bool _hasMoreScheduledStrikes;
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
        _nextStrikeElapsed = Mathf.Clamp(settings.initialStrikeDelay, 0f, _safeDuration);
        _safeStrikeInterval = Mathf.Max(MinContinuousStrikeSpacing, settings.strikeInterval);
        _emittedStrikeCount = 0;
        _hasMoreScheduledStrikes = true;
        _nextStrikeTime = _activationStartTime + _nextStrikeElapsed;

        Log(context.bow,
            $"Cast accepted. castOrigin={FormatVector(settings.castOrigin)} requestedCenter={FormatVector(center)} clampedCenter={FormatVector(_center)} maxTargetRange={settings.maxTargetRange:F2} zoneRadius={settings.zoneRadius:F2}");
        Log(context.bow,
            $"Started. center={FormatVector(_center)} zoneRadius={settings.zoneRadius:F2} duration={_safeDuration:F2} initialStrikeDelay={_nextStrikeElapsed:F2} strikeInterval={_safeStrikeInterval:F2}");
    }

    public void Tick(PlayerAbilityContext context)
    {
        if (!_isActive)
            return;

        float currentTime = Time.time;
        while (_hasMoreScheduledStrikes && currentTime >= _nextStrikeTime)
        {
            EmitScheduledStrike(context, _emittedStrikeCount == 0);
            _emittedStrikeCount++;

            _nextStrikeElapsed += _safeStrikeInterval;
            if (_nextStrikeElapsed > _safeDuration)
            {
                _hasMoreScheduledStrikes = false;
            }
            else
            {
                _nextStrikeTime = _activationStartTime + _nextStrikeElapsed;
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

        if (!_hasMoreScheduledStrikes && _pendingStrikes.Count == 0 && currentTime >= _activationEndTime)
            _isActive = false;
    }

    private void EmitScheduledStrike(PlayerAbilityContext context, bool isFirstStrike)
    {
        float finalDamage = context.bow != null
            ? context.bow.ResolveOutgoingDamage(_settings.damagePerStrike)
            : Mathf.Max(0f, _settings.damagePerStrike);
        Vector2 strikePoint = SelectStrikePoint(isFirstStrike);

        Log(context.bow,
            $"Strike emitted. center={FormatVector(_center)} firstStrike={isFirstStrike} emittedCount={_emittedStrikeCount + 1} damage={finalDamage:F2}");

        QueueStrike(context.bow, strikePoint, finalDamage);
    }

    private void QueueStrike(PlayerBowController playerBow, Vector2 strikePoint, float damage)
    {
        Vector2 clampedStrikePoint = ClampPointToRange(strikePoint, _settings);
        float strikeDistance = Vector2.Distance(_settings.castOrigin, clampedStrikePoint);
        Log(playerBow,
            $"Strike queued. requested={FormatVector(strikePoint)} clamped={FormatVector(clampedStrikePoint)} distanceFromCastOrigin={strikeDistance:F2} maxTargetRange={_settings.maxTargetRange:F2}");

        float impactDelay = SpawnVisual(clampedStrikePoint);
        SpawnImpactShadow(clampedStrikePoint, impactDelay);
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
        GameObject visualObject = null;
        GameplayPoolManager poolManager = GameplayPoolManager.Instance;
        if (poolManager != null)
        {
            PooledVisualInstance pooledVisual = poolManager.SpawnVisual(_settings.arrowRainVisualPrefab, spawnPoint, Quaternion.identity);
            if (pooledVisual != null)
                visualObject = pooledVisual.gameObject;
        }

        if (visualObject == null)
            visualObject = Object.Instantiate(_settings.arrowRainVisualPrefab, spawnPoint, Quaternion.identity);

        ArrowRainVisual visual = visualObject.GetComponent<ArrowRainVisual>();

        if (visual == null)
        {
            PooledVisualInstance pooledVisual = visualObject.GetComponent<PooledVisualInstance>();
            if (pooledVisual != null)
                pooledVisual.Despawn();
            else
                Object.Destroy(visualObject);
            return 0f;
        }

        visual.Initialize(
            spawnPoint,
            strikePoint,
            flightTime + VisualLifetimePadding);

        return flightTime;
    }

    private void SpawnImpactShadow(Vector2 strikePoint, float impactDelay)
    {
        if (!_settings.spawnImpactShadow || _settings.impactShadowPrefab == null)
            return;

        float safeDuration = Mathf.Max(0.01f, impactDelay);
        GameObject shadowObject = null;
        GameplayPoolManager poolManager = GameplayPoolManager.Instance;
        if (poolManager != null)
        {
            PooledVisualInstance pooledShadow = poolManager.SpawnVisual(_settings.impactShadowPrefab, strikePoint, Quaternion.identity);
            if (pooledShadow != null)
                shadowObject = pooledShadow.gameObject;
        }

        if (shadowObject == null)
            shadowObject = Object.Instantiate(_settings.impactShadowPrefab, strikePoint, Quaternion.identity);

        ArrowRainImpactShadowVisual shadowVisual =
            shadowObject.GetComponent<ArrowRainImpactShadowVisual>() ??
            shadowObject.AddComponent<ArrowRainImpactShadowVisual>();

        shadowVisual.Initialize(
            safeDuration,
            _settings.impactShadowStartScaleMultiplier,
            _settings.impactShadowEndScaleMultiplier,
            _settings.impactShadowStartAlpha,
            _settings.impactShadowEndAlpha);
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

    private Vector2 SelectStrikePoint(bool isFirstStrike)
    {
        if (isFirstStrike && _settings.guaranteeCenterStrike)
            return _center;

        float zoneRadius = Mathf.Max(0f, _settings.zoneRadius);
        if (zoneRadius <= 0f)
            return _center;

        return _center + (Random.insideUnitCircle * zoneRadius);
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
