using UnityEngine;

[CreateAssetMenu(fileName = "BloodMage_Attack_BubblePool", menuName = "Outland Haven/Enemy/Behaviors/Attack Logic/BloodMage Bubble Pool")]
public class BloodMageAttackSO : AttackSOBase<BloodMage>
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float castCooldown = 1.6f;

    [Header("Bubble Spell")]
    [SerializeField] private BloodMageBubbleSpell bubbleSpellPrefab;
    [SerializeField] private Vector2 bubbleTargetOffset = Vector2.zero;
    [SerializeField, Min(0f)] private float bubbleDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float bubbleKnockback = 1f;

    [Header("Bubble Targeting")]
    [SerializeField, Min(0f)] private float randomTargetRadius = 0.45f;

    private float _nextAllowedAttackTime;
    private bool _hasSpawnedBubble;
    private bool _hasHandledFinish;

    public bool IsComplete { get; private set; }
    public bool CanUseAttack => Time.time >= _nextAllowedAttackTime;

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        IsComplete = false;
        _hasSpawnedBubble = false;
        _hasHandledFinish = false;
        enemy.MoveEnemy(Vector2.zero);
        enemy.SetMovementAnimation(false);
        enemy.FaceAggroTarget();
#if UNITY_EDITOR
        enemy.DebugAttackLog($"BloodMage attack enter. cooldownReady={CanUseAttack} {enemy.GetAttackDebugTargetSummary()}");
#endif
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();
        enemy.MoveEnemy(Vector2.zero);
        enemy.FaceAggroTarget();
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
            if (_hasSpawnedBubble)
            {
#if UNITY_EDITOR
                enemy.DebugAttackLog("BloodMage duplicate Anim_AttackHit ignored.");
#endif
                return;
            }

            _hasSpawnedBubble = true;
#if UNITY_EDITOR
            enemy.DebugAttackLog($"BloodMage Anim_AttackHit -> spawning bubble. damage={enemy.AttackDamage * bubbleDamageMultiplier:0.##} knockback={bubbleKnockback:0.##} {enemy.GetAttackDebugTargetSummary()}");
#endif
            SpawnBubbleSpell();
            _nextAllowedAttackTime = Time.time + castCooldown;
#if UNITY_EDITOR
            enemy.DebugAttackLog($"BloodMage attack cooldown set. nextAllowed={_nextAllowedAttackTime:0.##}");
#endif
        }

        if (triggerType == Enemy.AnimationTriggerType.AttackFinished)
        {
            if (_hasHandledFinish)
            {
#if UNITY_EDITOR
                enemy.DebugAttackLog("BloodMage duplicate Anim_AttackFinished ignored.");
#endif
                return;
            }

            _hasHandledFinish = true;
            IsComplete = true;
#if UNITY_EDITOR
            enemy.DebugAttackLog("BloodMage Anim_AttackFinished -> attack complete.");
#endif
        }
    }

    public override void ResetValues()
    {
        base.ResetValues();
        IsComplete = false;
        _hasSpawnedBubble = false;
        _hasHandledFinish = false;
    }

    public void ResetRuntimeState()
    {
        IsComplete = false;
        _hasSpawnedBubble = false;
        _hasHandledFinish = false;
        _nextAllowedAttackTime = 0f;
    }

    private void SpawnBubbleSpell()
    {
        if (bubbleSpellPrefab == null)
        {
#if UNITY_EDITOR
            enemy.DebugAttackLog("BloodMage bubble spawn aborted: no bubbleSpellPrefab assigned.");
#endif
            return;
        }

        if (!enemy.TryGetAggroTargetPosition(out Vector2 targetPosition))
        {
#if UNITY_EDITOR
            enemy.DebugAttackLog("BloodMage bubble spawn aborted: no valid aggro target position.");
#endif
            return;
        }

        targetPosition = GetBubbleTargetPosition(targetPosition);
        Quaternion spawnRotation = Quaternion.identity;
        BloodMageBubbleSpell spawnedSpell = null;

        if (GameplayPoolManager.Instance != null)
        {
            spawnedSpell = GameplayPoolManager.Instance.SpawnProjectile(
                bubbleSpellPrefab,
                targetPosition,
                spawnRotation) as BloodMageBubbleSpell;
        }

        if (spawnedSpell == null)
        {
            // Safety fallback for scenes/tests without configured gameplay pools.
            spawnedSpell = Instantiate(bubbleSpellPrefab, targetPosition, spawnRotation);
            spawnedSpell.OnSpawned();
        }

        spawnedSpell.Initialize(
            targetPosition,
            enemy.AttackDamage * bubbleDamageMultiplier,
            bubbleKnockback,
            enemy.ProjectileIgnoreColliders,
            enemy.AggroTarget,
            enemy.name);

#if UNITY_EDITOR
        enemy.DebugAttackLog($"BloodMage bubble spawned at=({targetPosition.x:0.##},{targetPosition.y:0.##}) target={enemy.GetAttackDebugTargetSummary()}");
#endif
    }

    private Vector2 GetBubbleTargetPosition(Vector2 baseTargetPosition)
    {
        Vector2 targetPosition = baseTargetPosition;

        if (randomTargetRadius > 0f)
            targetPosition += Random.insideUnitCircle * randomTargetRadius;

        return targetPosition + bubbleTargetOffset;
    }
}
