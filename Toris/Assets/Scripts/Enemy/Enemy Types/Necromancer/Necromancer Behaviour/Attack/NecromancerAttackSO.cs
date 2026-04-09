using UnityEngine;

[CreateAssetMenu(fileName = "Necromancer_Attack_BoltCast", menuName = "Enemy Logic/Attack Logic/Necromancer Bolt Cast")]
public class NecromancerAttackSO : AttackSOBase<Necromancer>
{
    [Header("Timing")]
    [SerializeField] private float castCooldown = 1.5f;
    [SerializeField] private float panicSwingCooldown = 1f;
    [SerializeField] private float summonCooldown = 8f;

    [Header("Summon")]
    [SerializeField] private BloodMage bloodMageSummonPrefab;
    [SerializeField, Min(1)] private int bloodMageSummonCount = 3;
    [SerializeField, Min(0.1f)] private float bloodMageSummonRadius = 1.5f;
    [SerializeField] private float bloodMageSummonStartAngleDegrees = 90f;

    [Header("Spell Projectile")]
    [SerializeField] private NecromancerShotProjectile spellProjectilePrefab;
    [SerializeField, Min(0f)] private float spellProjectileDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float spellProjectileKnockback = 2f;
    [SerializeField, Min(0.01f)] private float spellProjectileSpeed = 6f;
    [SerializeField, Min(0.05f)] private float spellProjectileLifetime = 3f;

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
                SpawnSpellProjectile();

            if (enemy.PendingAttackType == NecromancerAttackType.PanicSwing)
                ApplyPanicSwingDamage();

            if (enemy.PendingAttackType == NecromancerAttackType.Summon)
                SpawnSummonedBloodMages();

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

    private void SpawnSpellProjectile()
    {
        if (spellProjectilePrefab == null)
        {
#if UNITY_EDITOR
            enemy.DebugAnimationDecision("SpellCast hit event fired, but no spell projectile prefab is assigned.");
#endif
            return;
        }

        Vector3 spawnPosition = enemy.CastPoint.position;
        Vector2 direction = enemy.GetDirectionToPlayer(spawnPosition);
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
            spawnedProjectile = Instantiate(spellProjectilePrefab, spawnPosition, spawnRotation);
            spawnedProjectile.OnSpawned();
        }

        spawnedProjectile.Initialize(
            direction,
            spellProjectileSpeed,
            enemy.AttackDamage * spellProjectileDamageMultiplier,
            spellProjectileLifetime,
            spellProjectileKnockback,
            enemy.ProjectileIgnoreColliders);

#if UNITY_EDITOR
        enemy.DebugAnimationLog(
            $"Spawned spell projectile at {spawnPosition} with speed={spellProjectileSpeed:0.##}, " +
            $"lifetime={spellProjectileLifetime:0.##}, damage={enemy.AttackDamage * spellProjectileDamageMultiplier:0.##}.");
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
            BloodMage spawnedBloodMage = SpawnBloodMage(i);
            if (spawnedBloodMage == null)
                continue;

            spawnedBloodMage.ConfigureSummon(enemy, i, bloodMageSummonCount);
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

    private BloodMage SpawnBloodMage(int summonIndex)
    {
        Vector3 spawnPosition = GetBloodMageSpawnPosition(summonIndex);
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
}
