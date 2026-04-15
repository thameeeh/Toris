using UnityEngine;

public class WolfChaseSO : ChaseSOBase<Wolf>
{
    [Header("Ranges")]
    [Tooltip("Beyond this distance, use pathfinding. Closer than this, use direct movement.")]
    [SerializeField] private float _pathChaseDistance = 4f;

    [Tooltip("Stop moving closer when within this distance to the player (prevents micro jitter in melee).")]
    [SerializeField] private float _stopDistance = 1.0f;

    [Header("Path Smoothing")]
    [Tooltip("Extra buffer so we don't keep flipping between path + direct every frame.")]
    [SerializeField] private float _pathHysteresis = 1.0f;

    private GridPathAgent _pathAgent;

    // remember whether we're currently in 'path mode'
    private bool _usingPath;

    public override void Initialize(GameObject gameObject, Wolf enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);

        _pathAgent = enemy.GetComponent<GridPathAgent>();
        if (_pathAgent == null)
        {
            Debug.LogWarning($"[WolfChaseSO] No GridPathAgent on {enemy.name}. Chase will not use pathfinding.");
        }

        _usingPath = false;
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.SetChasingPlayer(true);

        enemy.animator.SetBool("IsMoving", true);
        enemy.animator.Play("Run");

        // reset mode when entering chase
        _usingPath = false;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        Vector2 wolfPos = enemy.transform.position;
        Vector2 playerPos = playerTransform.position;
        Vector2 toPlayer = playerPos - wolfPos;

        float distToPlayer = toPlayer.magnitude;

        bool canHoldBitePosition = enemy.IsWithinStrikingDistance && distToPlayer <= _stopDistance;

        // 1. Only stop if the wolf is actually in striking range.
        // Otherwise it can park just outside the bite trigger and appear to run in place.
        if (canHoldBitePosition)
        {
            enemy.animator.SetBool("IsMoving", false);
            enemy.MoveEnemy(Vector2.zero);
            return;
        }

        // 2. Decide whether we should be in PATH mode or DIRECT mode (with hysteresis)
        if (_usingPath)
        {
            if (distToPlayer < _pathChaseDistance - _pathHysteresis)
            {
                _usingPath = false;
            }
        }
        else
        {
            if (distToPlayer > _pathChaseDistance + _pathHysteresis)
            {
                _usingPath = true;
            }
        }

        // 3. Compute movement
        Vector2 moveDir = Vector2.zero;

        if (_usingPath && _pathAgent != null)
        {
            moveDir = _pathAgent.GetMoveDirection(playerPos);

            if (moveDir.sqrMagnitude < 0.0001f)
            {
                moveDir = distToPlayer > 0.0001f ? (toPlayer / distToPlayer) : Vector2.zero;
            }
        }
        else
        {
            moveDir = distToPlayer > 0.0001f ? (toPlayer / distToPlayer) : Vector2.zero;
        }

        // 4. Apply movement
        float speed = enemy.MovementSpeed;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            enemy.animator.SetBool("IsMoving", true);
            enemy.MoveEnemy(moveDir * speed);
        }
        else
        {
            enemy.animator.SetBool("IsMoving", false);
            enemy.MoveEnemy(Vector2.zero);
        }
    }

    public override void DoExitLogic() 
    { 
        base.DoExitLogic();
        
        enemy.SetChasingPlayer(false);
    }
    public override void DoPhysicsLogic() { base.DoPhysicsLogic(); }
    public override void DoAnimationTriggerEventLogic(Enemy.AnimationTriggerType triggerType) { base.DoAnimationTriggerEventLogic(triggerType); }
    public override void ResetValues() { base.ResetValues(); }
}
