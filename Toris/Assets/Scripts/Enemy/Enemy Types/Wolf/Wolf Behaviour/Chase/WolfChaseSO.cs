using UnityEngine;

[CreateAssetMenu(fileName = "Wolf_Chase_Direct", menuName = "Enemy Logic/Chase Logic/Wolf Chase Direct")]
public class WolfChaseSO : ChaseSOBase<Wolf>
{
    [Header("Movement")]
    [SerializeField] private float _movementSpeed = 2f;

    [Tooltip("Radius of the circle/arc around the player where wolves should stand.")]
    [SerializeField] private float _slotRadius = 1.75f;

    [Tooltip("How close to the desired slot is 'good enough' to stop (prevents jitter).")]
    [SerializeField] private float _arrivalDistance = 0.2f;

    [Header("Formation")]
    [Tooltip("How wide the pack arc is in degrees (<= 180 to keep them on one side).")]
    [SerializeField] private float _arcAngle = 120f;

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.animator.SetBool("IsMoving", true);
        enemy.animator.Play("Run");
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        Vector2 moveDirection = GetMoveDirection();

        // If MoveEnemy already uses Time.deltaTime internally, keep this.
        // Otherwise, change to: enemy.MoveEnemy(moveDirection * _movementSpeed * Time.deltaTime);
        enemy.MoveEnemy(moveDirection * _movementSpeed);
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
    }

    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType)
    {
        base.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void ResetValues()
    {
        base.ResetValues();
    }

    private Vector2 GetMoveDirection()
    {
        Vector2 playerPos = playerTransform.position;
        Vector2 wolfPos = enemy.transform.position;

        Vector2 slotOffset = GetSlotOffsetArc();
        Vector2 desiredPosition = playerPos + slotOffset;

        Vector2 toTarget = desiredPosition - wolfPos;

        if (toTarget.sqrMagnitude <= _arrivalDistance * _arrivalDistance)
            return Vector2.zero;

        if (toTarget == Vector2.zero)
            return Vector2.zero;

        return toTarget.normalized;
    }

    /// <summary>
    /// Returns the offset for this wolf's slot,
    /// distributed in an arc on ONE SIDE of the player,
    /// centered on direction from player -> pack leader.
    /// </summary>
    private Vector2 GetSlotOffsetArc()
    {
        Wolf[] packMembers = GetPackMembers();
        if (packMembers.Length == 0) return Vector2.zero;

        int slotIndex = GetSlotIndex(packMembers);

        Transform anchorTransform = enemy.transform;

        if (enemy.pack != null && enemy.pack.leaderWolf != null)
        {
            anchorTransform = enemy.pack.leaderWolf.transform;
        }

        Vector2 playerPos = playerTransform.position;
        Vector2 anchorPos = anchorTransform.position;

        Vector2 baseDir = anchorPos - playerPos;
        if (baseDir.sqrMagnitude < 0.0001f)
        {
            baseDir = Vector2.right;
        }

        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;

        float clampedArc = Mathf.Clamp(_arcAngle, 0f, 180f);
        int count = Mathf.Max(1, packMembers.Length);

        float angleStep = (count > 1) ? clampedArc / (count - 1) : 0f;
        float startAngle = baseAngle - clampedArc * 0.5f;
        float angle = startAngle + angleStep * slotIndex;

        Vector2 offsetDirection = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );

        return offsetDirection * _slotRadius;
    }

    private int GetSlotIndex(Wolf[] packMembers)
    {
        for (int i = 0; i < packMembers.Length; i++)
        {
            if (packMembers[i] == enemy)
                return i;
        }

        int fallbackIndex = Mathf.Abs(enemy.GetInstanceID());
        return fallbackIndex % Mathf.Max(1, packMembers.Length);
    }

    private Wolf[] GetPackMembers()
    {
        if (enemy.pack == null)
        {
            return new[] { enemy };
        }

        var members = enemy.pack.activeMinions;

        int capacity = 1 + members.Count;
        Wolf[] packMembers = new Wolf[capacity];
        int count = 0;

        if (enemy.pack.leaderWolf != null)
        {
            packMembers[count++] = enemy.pack.leaderWolf;
        }

        for (int i = 0; i < members.Count; i++)
        {
            Wolf minion = members[i];
            if (minion != null)
            {
                packMembers[count++] = minion;
            }
        }

        if (count == packMembers.Length) return packMembers;

        Wolf[] trimmed = new Wolf[count];
        for (int i = 0; i < count; i++)
        {
            trimmed[i] = packMembers[i];
        }

        return trimmed;
    }
}
