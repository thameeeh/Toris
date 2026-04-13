using UnityEngine;

[CreateAssetMenu(fileName = "Necromancer_Attack_BoltCast", menuName = "Enemy Logic/Attack Logic/Necromancer Bolt Cast")]
public class NecromancerAttackSO : AttackSOBase<Necromancer>
{
    private const float FullCircleDegrees = 360f;

    [Header("Timing")]
    [SerializeField] private float castCooldown = 1.5f;
    [SerializeField] private float panicSwingCooldown = 1f;
    [SerializeField] private float summonCooldown = 8f;

    [Header("Summon")]
    [SerializeField] private BloodMage bloodMageSummonPrefab;
    [SerializeField] private BloodMageSpawnEffect bloodMageSpawnEffectPrefab;
    [SerializeField, Min(1)] private int bloodMageSummonCount = 3;
    [SerializeField, Min(0.1f)] private float bloodMageSummonRadius = 1.5f;
    [SerializeField] private float bloodMageSummonStartAngleDegrees = 90f;

    [Header("Spell Projectile")]
    [SerializeField] private NecromancerShotProjectile spellProjectilePrefab;
    [SerializeField, Min(0f)] private float spellProjectileDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float spellProjectileKnockback = 2f;
    [SerializeField, Min(0.01f)] private float spellProjectileSpeed = 6f;
    [SerializeField, Min(0.05f)] private float spellProjectileLifetime = 3f;

    [Header("Phase Two Spell Volley")]
    [SerializeField] private bool enablePhaseTwoSpellVolley = true;
    [SerializeField, Min(1)] private int phaseTwoSpellProjectileCount = 5;
    [SerializeField, Range(0f, 180f)] private float phaseTwoSpellSpreadAngle = 24f;
    [SerializeField, Min(0f)] private float phaseTwoSpellProjectileDamageMultiplier = 0.6f;

    [Header("Summon Projectile Burst")]
    [SerializeField] private bool enableSummonProjectileBurst = true;
    [SerializeField, Min(1)] private int summonProjectileBurstCount = 8;
    [SerializeField, Min(0f)] private float summonProjectileBurstDamageMultiplier = 0.35f;

    [Header("Summon Impact Hold")]
    [SerializeField] private bool enableSummonImpactHold = true;
    [SerializeField, Min(0f)] private float summonImpactHoldDuration = 0.15f;

    [Header("Panic Swing")]
    [SerializeField, Min(0f)] private float panicSwingDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float panicSwingKnockback = 2f;

    [Header("Reposition")]
    [SerializeField, Min(1f)] private float spellCastRepositionSpeedMultiplier = 1f;
    [SerializeField, Min(1f)] private float panicSwingRepositionSpeedMultiplier = 2f;

    public bool IsComplete { get; private set; }

    private float _nextAllowedCastTime;
    private float _nextAllowedPanicSwingTime;
    private float _nextAllowedSummonTime;

    public bool CanUseAttack(NecromancerAttackType attackType)
    {
        float now = Time.time;

        switch (attackType)
        {
            case NecromancerAttackType.PanicSwing:
                return now >= _nextAllowedPanicSwingTime;
            case NecromancerAttackType.Summon:
                return now >= _nextAllowedSummonTime;
            default:
                return now >= _nextAllowedCastTime;
        }
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        IsComplete = false;
        enemy.MoveEnemy(Vector2.zero);
        enemy.SetMovementAnimation(false);
        enemy.FacePlayer();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        enemy.MoveEnemy(Vector2.zero);
        enemy.FacePlayer();
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
        enemy.MoveEnemy(Vector2.zero);
    }

    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);

        if (triggerType == Enemy.AnimationTriggerType.Attack)
        {
#if UNITY_EDITOR
            enemy.DebugAnimationLog($"Animation event -> Anim_AttackHit for {enemy.PendingAttackType}. Starting cooldown.");
#endif
            if (enemy.PendingAttackType == NecromancerAttackType.SpellCast)
                SpawnSpellProjectiles();

            if (enemy.PendingAttackType == NecromancerAttackType.PanicSwing)
                ApplyPanicSwingDamage();

            if (enemy.PendingAttackType == NecromancerAttackType.Summon)
            {
                SpawnSummonedBloodMages();
                SpawnSummonProjectileBurst();

                if (enableSummonImpactHold && summonImpactHoldDuration > 0f)
                    enemy.BeginTemporaryAnimatorHold(summonImpactHoldDuration);
            }

            if (enemy.PendingAttackType != NecromancerAttackType.Summon)
                StartCooldown(enemy.PendingAttackType);
        }

        if (triggerType == Enemy.AnimationTriggerType.AttackFinished)
        {
#if UNITY_EDITOR
            enemy.DebugAnimationLog("Animation event -> Anim_AttackFinished. Marking attack complete.");
#endif
            if (enemy.PendingAttackType == NecromancerAttackType.SpellCast)
                enemy.RequirePostCastReposition(spellCastRepositionSpeedMultiplier);

            if (enemy.PendingAttackType == NecromancerAttackType.PanicSwing)
                enemy.RequirePostCastReposition(panicSwingRepositionSpeedMultiplier, true);

            IsComplete = true;
        }
    }

    public override void ResetValues()
    {
        base.ResetValues();
        IsComplete = false;
    }

    public void ResetRuntimeState()
    {
        IsComplete = false;
        _nextAllowedCastTime = 0f;
        _nextAllowedPanicSwingTime = 0f;
        _nextAllowedSummonTime = 0f;
    }

    public void StartSummonCooldownFromProtectionLoss()
    {
        _nextAllowedSummonTime = Time.time + summonCooldown;
    }

    private void StartCooldown(NecromancerAttackType attackType)
    {
        float nextAllowedTime = Time.time + GetCooldown(attackType);

        switch (attackType)
        {
            case NecromancerAttackType.PanicSwing:
                _nextAllowedPanicSwingTime = nextAllowedTime;
                return;
            case NecromancerAttackType.Summon:
                _nextAllowedSummonTime = nextAllowedTime;
                return;
            default:
                _nextAllowedCastTime = nextAllowedTime;
                return;
        }
    }

    private float GetCooldown(NecromancerAttackType attackType)
    {
        switch (attackType)
        {
            case NecromancerAttackType.PanicSwing:
                return panicSwingCooldown;
            case NecromancerAttackType.Summon:
                return summonCooldown;
            default:
                return castCooldown;
        }
    }

    private void SpawnSpellProjectiles()
    {
        if (spellProjectilePrefab == null)
        {
#if UNITY_EDITOR
            enemy.DebugAnimationDecision("SpellCast hit event fired, but no spell projectile prefab is assigned.");
#endif
            return;
        }

        Vector3 spawnPosition = enemy.CastPoint.position;
        Vector2 forwardDirection = GetSpellForwardDirection(spawnPosition);
        if (forwardDirection.sqrMagnitude <= 0f)
            return;

        if (!ShouldUsePhaseTwoSpellVolley())
        {
            SpawnProjectile(spawnPosition, forwardDirection, spellProjectileDamageMultiplier);
            return;
        }

        int projectileCount = Mathf.Max(1, phaseTwoSpellProjectileCount);
        if (projectileCount == 1)
        {
            SpawnProjectile(spawnPosition, forwardDirection, phaseTwoSpellProjectileDamageMultiplier);
            return;
        }

        float halfSpread = phaseTwoSpellSpreadAngle * 0.5f;
        float angleStep = projectileCount > 1 ? phaseTwoSpellSpreadAngle / (projectileCount - 1) : 0f;

        for (int i = 0; i < projectileCount; i++)
        {
            float angleOffset = -halfSpread + (angleStep * i);
            Vector2 projectileDirection = RotateDirection(forwardDirection, angleOffset);
            SpawnProjectile(spawnPosition, projectileDirection, phaseTwoSpellProjectileDamageMultiplier);
        }

 #if UNITY_EDITOR
        enemy.DebugAnimationLog(
            $"Spawned phase-two spell volley count={projectileCount}, spread={phaseTwoSpellSpreadAngle:0.##}, " +
            $"damageMult={phaseTwoSpellProjectileDamageMultiplier:0.##}.");
#endif
    }

    private void SpawnSummonProjectileBurst()
    {
        if (!enableSummonProjectileBurst || spellProjectilePrefab == null)
            return;

        int projectileCount = Mathf.Max(1, summonProjectileBurstCount);
        Vector3 spawnPosition = enemy.CastPoint.position;
        Vector2 baseDirection = GetSpellForwardDirection(spawnPosition);
        if (baseDirection.sqrMagnitude <= 0f)
            return;

        float angleStep = FullCircleDegrees / projectileCount;
        for (int i = 0; i < projectileCount; i++)
        {
            float angleOffset = angleStep * i;
            Vector2 projectileDirection = RotateDirection(baseDirection, angleOffset);
            SpawnProjectile(spawnPosition, projectileDirection, summonProjectileBurstDamageMultiplier);
        }

#if UNITY_EDITOR
        enemy.DebugAnimationLog(
            $"Summon released projectile burst count={projectileCount}, " +
            $"damageMult={summonProjectileBurstDamageMultiplier:0.##}.");
#endif
    }

    private void ApplyPanicSwingDamage()
    {
        Vector2 hitDirection = enemy.GetDirectionToPlayer(enemy.transform.position);
        float damageAmount = enemy.AttackDamage * panicSwingDamageMultiplier;
        HitData hitData = new HitData(
            enemy.transform.position,
            hitDirection,
            damageAmount,
            panicSwingKnockback,
            enemy.gameObject);

        enemy.DamagePlayer(damageAmount, hitData);

#if UNITY_EDITOR
        enemy.DebugAnimationLog(
            $"Applied PanicSwing damage={damageAmount:0.##}, knockback={panicSwingKnockback:0.##}.");
#endif
    }

    private void SpawnSummonedBloodMages()
    {
        if (bloodMageSummonPrefab == null)
        {
#if UNITY_EDITOR
            enemy.DebugAnimationDecision("Summon hit event fired, but no Blood Mage summon prefab is assigned.");
#endif
            return;
        }

        if (bloodMageSummonCount <= 0)
        {
#if UNITY_EDITOR
            enemy.DebugAnimationDecision("Summon hit event fired, but summon count is zero.");
#endif
            return;
        }

        enemy.MarkSummonProtectionPending();

        int spawnedCount = 0;
        for (int i = 0; i < bloodMageSummonCount; i++)
        {
            bool didStartSpawn = StartBloodMageSpawn(i);
            if (!didStartSpawn)
                continue;

            spawnedCount++;
        }

        if (spawnedCount == 0)
        {
            enemy.ClearSummonProtection();

#if UNITY_EDITOR
            enemy.DebugAnimationDecision("Summon completed, but no Blood Mages spawned. Clearing summon protection state.");
#endif
            return;
        }

#if UNITY_EDITOR
        enemy.DebugAnimationLog(
            $"Summon spawned {spawnedCount}/{bloodMageSummonCount} Blood Mages at radius={bloodMageSummonRadius:0.##}.");
#endif
    }

    private bool StartBloodMageSpawn(int summonIndex)
    {
        Vector3 spawnPosition = GetBloodMageSpawnPosition(summonIndex);
        if (bloodMageSpawnEffectPrefab != null)
            return SpawnBloodMageSpawnEffect(summonIndex, spawnPosition);

        BloodMage spawnedBloodMage = SpawnBloodMageDirect(spawnPosition);
        if (spawnedBloodMage == null)
            return false;

        spawnedBloodMage.ConfigureSummon(enemy, summonIndex, bloodMageSummonCount);
        return true;
    }

    private bool SpawnBloodMageSpawnEffect(int summonIndex, Vector3 spawnPosition)
    {
        Quaternion spawnRotation = Quaternion.identity;
        BloodMageSpawnEffect spawnedEffect = null;

        if (GameplayPoolManager.Instance != null)
        {
            spawnedEffect = GameplayPoolManager.Instance.SpawnProjectile(
                bloodMageSpawnEffectPrefab,
                spawnPosition,
                spawnRotation) as BloodMageSpawnEffect;
        }

        if (spawnedEffect == null)
        {
            // Safety fallback for scenes/tests without configured gameplay pools.
            spawnedEffect = Instantiate(bloodMageSpawnEffectPrefab, spawnPosition, spawnRotation);
            spawnedEffect.OnSpawned();
        }

        spawnedEffect.Initialize(
            bloodMageSummonPrefab,
            enemy,
            spawnPosition,
            summonIndex,
            bloodMageSummonCount);

        return true;
    }

    private BloodMage SpawnBloodMageDirect(Vector3 spawnPosition)
    {
        Quaternion spawnRotation = Quaternion.identity;
        BloodMage spawnedBloodMage = null;

        if (GameplayPoolManager.Instance != null)
        {
            spawnedBloodMage = GameplayPoolManager.Instance.SpawnEnemy(
                bloodMageSummonPrefab,
                spawnPosition,
                spawnRotation) as BloodMage;
        }

        if (spawnedBloodMage == null)
        {
            // Safety fallback for scenes/tests without configured gameplay pools.
            spawnedBloodMage = Instantiate(bloodMageSummonPrefab, spawnPosition, spawnRotation);
            spawnedBloodMage.OnSpawned();
        }

        return spawnedBloodMage;
    }

    private Vector3 GetBloodMageSpawnPosition(int summonIndex)
    {
        float angleStep = 360f / bloodMageSummonCount;
        float angleDegrees = bloodMageSummonStartAngleDegrees + (angleStep * summonIndex);
        float angleRadians = angleDegrees * Mathf.Deg2Rad;

        Vector2 offset = new Vector2(
            Mathf.Cos(angleRadians),
            Mathf.Sin(angleRadians)) * bloodMageSummonRadius;

        return enemy.transform.position + (Vector3)offset;
    }

    private bool ShouldUsePhaseTwoSpellVolley()
    {
        return enablePhaseTwoSpellVolley
            && enemy.IsPhaseTwoSummonUnlocked
            && phaseTwoSpellProjectileCount > 1;
    }

    private Vector2 GetSpellForwardDirection(Vector3 spawnPosition)
    {
        Vector2 direction = enemy.GetDirectionToPlayer(spawnPosition);
        if (direction.sqrMagnitude > 0.0001f)
            return direction;

        return enemy.IsFacingRight ? Vector2.right : Vector2.left;
    }

    private void SpawnProjectile(Vector3 spawnPosition, Vector2 direction, float damageMultiplier)
    {
        Quaternion spawnRotation = Quaternion.identity;
        NecromancerShotProjectile spawnedProjectile = null;

        if (GameplayPoolManager.Instance != null)
        {
            spawnedProjectile = GameplayPoolManager.Instance.SpawnProjectile(
                spellProjectilePrefab,
                spawnPosition,
                spawnRotation) as NecromancerShotProjectile;
        }

        if (spawnedProjectile == null)
        {
            // Safety fallback for scenes/tests without configured gameplay pools.
            spawnedProjectile = Instantiate(spellProjectilePrefab, spawnPosition, spawnRotation);
            spawnedProjectile.OnSpawned();
        }

        spawnedProjectile.Initialize(
            direction,
            spellProjectileSpeed,
            enemy.AttackDamage * damageMultiplier,
            spellProjectileLifetime,
            spellProjectileKnockback,
            enemy.ProjectileIgnoreColliders);
    }

    private static Vector2 RotateDirection(Vector2 direction, float angleDegrees)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angleRadians);
        float sin = Mathf.Sin(angleRadians);

        return new Vector2(
            (direction.x * cos) - (direction.y * sin),
            (direction.x * sin) + (direction.y * cos)).normalized;
    }
}
