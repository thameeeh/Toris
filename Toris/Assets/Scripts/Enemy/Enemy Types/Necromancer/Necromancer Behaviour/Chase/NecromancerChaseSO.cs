using UnityEngine;

[CreateAssetMenu(fileName = "Necromancer_Chase_CastRange", menuName = "Enemy Logic/Chase Logic/Necromancer Cast Range Chase")]
public class NecromancerChaseSO : ChaseSOBase<Necromancer>
{
    [Header("Ranges")]
    [SerializeField] private float preferredDistance = 6f;
    [SerializeField] private float retreatDistance = 4f;

    [Header("Pathing")]
    [SerializeField] private float minMoveDirectionSqr = 0.0001f;
    [SerializeField] private float floaterMoveSpeedMultiplier = 0.8f;

    [Header("Post Cast Reposition")]
    [SerializeField] private float postCastRepositionDuration = 0.75f;

    private GridPathAgent _pathAgent;
    private float _preferredDistanceSqr;
    private float _retreatDistanceSqr;
    private float _postCastRepositionTimer;
    private bool _isPostCastRepositioning;

    public bool CanStartSelectedAttack { get; private set; }
    public NecromancerAttackType SelectedAttackType { get; private set; }
    public bool CanCastSpell => CanStartSelectedAttack && SelectedAttackType == NecromancerAttackType.SpellCast;
    public bool IsInPanicRange { get; private set; }
    public bool IsInCastingRange { get; private set; }

    public override void Initialize(GameObject gameObject, Necromancer enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
        enemy.TryGetComponent(out _pathAgent);
        CacheSquaredRanges();
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        CacheSquaredRanges();
        ResetAttackSelection();
        IsInPanicRange = false;
        IsInCastingRange = false;
        _postCastRepositionTimer = enemy.RequiresPostCastReposition ? postCastRepositionDuration : 0f;
        _isPostCastRepositioning = enemy.RequiresPostCastReposition;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();

        ResetAttackSelection();
        IsInPanicRange = false;
        IsInCastingRange = false;
        enemy.SetMovementAnimation(false);
        enemy.MoveEnemy(Vector2.zero);
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        ResetAttackSelection();
        IsInPanicRange = false;
        IsInCastingRange = false;

        if (playerTransform == null)
        {
            StopMoving();
            return;
        }

        Vector2 enemyPosition = enemy.transform.position;
        Vector2 playerPosition = playerTransform.position;
        Vector2 toPlayer = playerPosition - enemyPosition;
        float distanceToPlayerSqr = toPlayer.sqrMagnitude;

        if (enemy.IsHumanRescueVariant)
        {
            MoveAwayFromPlayer(toPlayer);
            return;
        }

        if (enemy.IsChangingForm)
        {
            StopMoving();
            enemy.FacePlayer();
            return;
        }

        if (UpdatePostCastReposition(toPlayer, distanceToPlayerSqr))
            return;

        if (enemy.IsWithinStrikingDistance)
        {
            IsInPanicRange = true;
            StopMoving();
            enemy.FacePlayer();

            if (enemy.IsHumanForm)
            {
                enemy.RequestBecomeFloater();
                return;
            }

            SelectAttack(NecromancerAttackType.PanicSwing);
            return;
        }

        if (distanceToPlayerSqr < _retreatDistanceSqr)
        {
            if (enemy.IsHumanForm)
            {
                StopMoving();
                enemy.FacePlayer();
                enemy.RequestBecomeFloater();
                return;
            }

            MoveAwayFromPlayer(toPlayer);
            return;
        }

        IsInCastingRange = enemy.IsWithinCastingRange;

        if (IsInCastingRange)
        {
            StopMoving();
            enemy.FacePlayer();

            if (enemy.IsHumanForm)
            {
                enemy.RequestBecomeFloater();
                return;
            }

            SelectAttack(enemy.IsPhaseTwoSummonUnlocked && enemy.CanStartAttack(NecromancerAttackType.Summon)
                ? NecromancerAttackType.Summon
                : NecromancerAttackType.SpellCast);
            return;
        }

        MoveTowardPlayer(playerPosition, toPlayer);
    }

    public override void ResetValues()
    {
        base.ResetValues();
        ResetAttackSelection();
        IsInPanicRange = false;
        IsInCastingRange = false;
        _postCastRepositionTimer = 0f;
        _isPostCastRepositioning = false;
    }

    private void StopMoving()
    {
        enemy.SetMovementAnimation(false);
        enemy.MoveEnemy(Vector2.zero);
    }

    private void MoveTowardPlayer(Vector2 playerPosition, Vector2 toPlayer)
    {
        Vector2 moveDirection = Vector2.zero;
        if (_pathAgent != null)
            moveDirection = _pathAgent.GetMoveDirection(playerPosition);

        if (moveDirection.sqrMagnitude <= minMoveDirectionSqr)
            moveDirection = toPlayer.sqrMagnitude > minMoveDirectionSqr ? toPlayer.normalized : Vector2.zero;

        MoveInDirection(moveDirection);
    }

    private void MoveAwayFromPlayer(Vector2 toPlayer)
    {
        Vector2 moveDirection = toPlayer.sqrMagnitude > minMoveDirectionSqr ? -toPlayer.normalized : Vector2.zero;
        MoveInDirection(moveDirection);
    }

    private void MoveInDirection(Vector2 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= minMoveDirectionSqr)
        {
            StopMoving();
            return;
        }

        enemy.SetMovementAnimation(enemy.IsHumanForm);
        enemy.MoveEnemy(moveDirection.normalized * GetCurrentMoveSpeed());
    }

    private bool UpdatePostCastReposition(Vector2 toPlayer, float distanceToPlayerSqr)
    {
        if (!_isPostCastRepositioning && enemy.RequiresPostCastReposition)
        {
            _isPostCastRepositioning = true;
            _postCastRepositionTimer = postCastRepositionDuration;
        }

        if (!_isPostCastRepositioning)
            return false;

        _postCastRepositionTimer -= Time.deltaTime;
        MoveAwayFromPlayer(toPlayer);

        if (_postCastRepositionTimer <= 0f && distanceToPlayerSqr >= _preferredDistanceSqr)
        {
            _isPostCastRepositioning = false;
            enemy.ClearPostCastReposition();
        }

        return true;
    }

    private float GetCurrentMoveSpeed()
    {
        if (enemy.IsFloaterForm || enemy.IsReadyToCastAnimation)
            return enemy.MovementSpeed * floaterMoveSpeedMultiplier;

        return enemy.MovementSpeed;
    }

    private void CacheSquaredRanges()
    {
        _preferredDistanceSqr = preferredDistance * preferredDistance;
        _retreatDistanceSqr = retreatDistance * retreatDistance;
    }

    private void SelectAttack(NecromancerAttackType attackType)
    {
        SelectedAttackType = attackType;
        CanStartSelectedAttack = enemy.IsReadyToCastAnimation && enemy.CanStartAttack(attackType);
    }

    private void ResetAttackSelection()
    {
        SelectedAttackType = NecromancerAttackType.SpellCast;
        CanStartSelectedAttack = false;
    }
}
