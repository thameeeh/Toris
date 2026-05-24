using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Attack_QuickBite", menuName = "Enemy Logic/Attack Logic/Wolf Attack QuickBite")]
public class WolfAttackSO : AttackSOBase<Wolf>
{
    public bool isComplete {  get; private set; }

    [Header("SFX")]
    [SerializeField] private string attackGrowlSfxId = "enemy_wolf_attack_growl";
    [SerializeField] private string biteHitSfxId = "enemy_wolf_bite_hit";
    [SerializeField] private bool force2D = false;

    private GridPathAgent _pathAgent;
    private bool _hasAppliedHit;
    private bool _hasHandledFinish;

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);

        _pathAgent = enemy.GetComponent<GridPathAgent>();
        if (_pathAgent == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[WolfAttackSO] No GridPathAgent on {enemy.name}. Attack lunge will not use pathfinding.");
#endif
        }
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        isComplete = false;
        _hasAppliedHit = false;
        _hasHandledFinish = false;
        enemy.IsMovingWhileBiting = false;
#if UNITY_EDITOR
        enemy.DebugAttackLog($"Wolf bite enter. movingWhileBiting={enemy.IsMovingWhileBiting} {enemy.GetAttackDebugTargetSummary()}");
#endif
        PlayAttackGrowlSfx();
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
        enemy.IsMovingWhileBiting = false;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (!enemy.IsMovingWhileBiting)
        {
            enemy.MoveEnemy(Vector2.zero);
            return;
        }

        Vector2 moveDirection = Vector2.zero;

        Transform targetTransform = enemy.AggroTargetTransform;
        if (_pathAgent != null && TileNavWorld.Instance != null && targetTransform != null)
            moveDirection = _pathAgent.GetMoveDirection(targetTransform.position);

        if (moveDirection.sqrMagnitude > 0.0001f)
            enemy.MoveEnemy(moveDirection * enemy.MovementSpeed);
        else
            enemy.MoveEnemy(Vector2.zero);
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);

        if (triggerType == Enemy.AnimationTriggerType.Attack)
        {
            if (_hasAppliedHit)
            {
#if UNITY_EDITOR
                enemy.DebugAttackLog("Wolf bite duplicate Anim_AttackHit ignored.");
#endif
                return;
            }

            _hasAppliedHit = true;
#if UNITY_EDITOR
            enemy.DebugAttackLog($"Wolf bite Anim_AttackHit -> attempting damage {enemy.AttackDamage:0.##}. striking={enemy.IsWithinStrikingDistance} {enemy.GetAttackDebugTargetSummary()}");
#endif
            bool didHit = enemy.DamageCurrentTarget(enemy.AttackDamage);
            if (didHit)
                PlayBiteHitSfx();
        }

        if (triggerType == Enemy.AnimationTriggerType.AttackFinished)
        {
            if (_hasHandledFinish)
            {
#if UNITY_EDITOR
                enemy.DebugAttackLog("Wolf bite duplicate Anim_AttackFinished ignored.");
#endif
                return;
            }

            _hasHandledFinish = true;
            isComplete = true;
            enemy.IsMovingWhileBiting = false;
#if UNITY_EDITOR
            enemy.DebugAttackLog("Wolf bite Anim_AttackFinished -> attack complete.");
#endif
        }
    }

    public override void ResetValues()
    {
        base.ResetValues();

        isComplete = false;
        _hasAppliedHit = false;
        _hasHandledFinish = false;
    }

    private void PlayAttackGrowlSfx()
    {
        // SFX-only hook: wolf attack-commit audio plays when the attack state starts.
        // It must not affect aggro, movement, hit timing, damage, or state completion.
        PlaySfx(attackGrowlSfxId);
    }

    private void PlayBiteHitSfx()
    {
        // SFX-only hook: bite impact audio plays only after the attack hit path accepts a target.
        // It must not affect damage, i-frames, knockback, or attack state transitions.
        PlaySfx(biteHitSfxId);
    }

    private void PlaySfx(string sfxId)
    {
        if (enemy == null || AudioBootstrap.Sfx == null || string.IsNullOrWhiteSpace(sfxId))
            return;

        var request = SfxPlayRequest.Default;
        request.force2D = force2D;

        AudioBootstrap.Sfx.PlayAttached(sfxId, enemy.transform, Vector3.zero, request);
    }
}
