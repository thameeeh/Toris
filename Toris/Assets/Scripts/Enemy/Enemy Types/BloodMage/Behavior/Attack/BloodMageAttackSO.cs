using UnityEngine;

[CreateAssetMenu(fileName = "BloodMage_Attack_BubblePool", menuName = "Enemy Logic/Attack Logic/BloodMage Bubble Pool")]
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

    public bool IsComplete { get; private set; }
    public bool CanUseAttack => Time.time >= _nextAllowedAttackTime;

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
            SpawnBubbleSpell();
            _nextAllowedAttackTime = Time.time + castCooldown;
        }

        if (triggerType == Enemy.AnimationTriggerType.AttackFinished)
            IsComplete = true;
    }

    public override void ResetValues()
    {
        base.ResetValues();
        IsComplete = false;
    }

    public void ResetRuntimeState()
    {
        IsComplete = false;
        _nextAllowedAttackTime = 0f;
    }

    private void SpawnBubbleSpell()
    {
        if (bubbleSpellPrefab == null || enemy.PlayerTransform == null)
            return;

        Vector2 targetPosition = GetBubbleTargetPosition();
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
            enemy.ProjectileIgnoreColliders);
    }

    private Vector2 GetBubbleTargetPosition()
    {
        Vector2 targetPosition = (Vector2)enemy.PlayerTransform.position;

        if (randomTargetRadius > 0f)
            targetPosition += Random.insideUnitCircle * randomTargetRadius;

        return targetPosition + bubbleTargetOffset;
    }
}
