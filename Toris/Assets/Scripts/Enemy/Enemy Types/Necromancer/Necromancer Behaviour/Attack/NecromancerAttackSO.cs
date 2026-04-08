using UnityEngine;

[CreateAssetMenu(fileName = "Necromancer_Attack_BoltCast", menuName = "Enemy Logic/Attack Logic/Necromancer Bolt Cast")]
public class NecromancerAttackSO : AttackSOBase<Necromancer>
{
    [Header("Timing")]
    [SerializeField] private float castCooldown = 1.5f;
    [SerializeField] private float panicSwingCooldown = 1f;
    [SerializeField] private float summonCooldown = 8f;

    [Header("Spell Projectile")]
    [SerializeField] private NecromancerShotProjectile spellProjectilePrefab;
    [SerializeField, Min(0f)] private float spellProjectileDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float spellProjectileKnockback = 2f;
    [SerializeField, Min(0.01f)] private float spellProjectileSpeed = 6f;
    [SerializeField, Min(0.05f)] private float spellProjectileLifetime = 3f;

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

            if (enemy.PendingAttackType == NecromancerAttackType.Summon)
                enemy.MarkSummonProtectionPending();

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
}
