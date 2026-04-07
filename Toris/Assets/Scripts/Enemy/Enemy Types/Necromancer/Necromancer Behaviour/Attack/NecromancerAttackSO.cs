using UnityEngine;

[CreateAssetMenu(fileName = "Necromancer_Attack_BoltCast", menuName = "Enemy Logic/Attack Logic/Necromancer Bolt Cast")]
public class NecromancerAttackSO : AttackSOBase<Necromancer>
{
    [Header("Timing")]
    [SerializeField] private float castCooldown = 1.5f;
    [SerializeField] private float panicSwingCooldown = 1f;
    [SerializeField] private float summonCooldown = 8f;

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
            StartCooldown(enemy.PendingAttackType);
        }

        if (triggerType == Enemy.AnimationTriggerType.AttackFinished)
        {
#if UNITY_EDITOR
            enemy.DebugAnimationLog("Animation event -> Anim_AttackFinished. Marking attack complete.");
#endif
            if (enemy.PendingAttackType == NecromancerAttackType.SpellCast)
                enemy.RequirePostCastReposition();

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
}
