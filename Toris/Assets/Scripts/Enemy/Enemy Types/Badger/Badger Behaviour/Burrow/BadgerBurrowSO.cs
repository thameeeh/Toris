using UnityEngine;

[CreateAssetMenu(fileName = "Badger_Burrow", menuName = "Enemy Logic/Burrow Logic/Badger Burrow")]
public class BadgerBurrowSO : BurrowSO<Badger>
{
    [SerializeField] private float burrowIdleTime = 0.5f;
    private float _burrowTimer;
    public override void Initialize(GameObject gameObject, Badger enemy, Transform player)
    {
        base.Initialize(gameObject, enemy, player);
    }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.isBurrowed = true;
        enemy.isTunneling = false;
        enemy.MoveEnemy(Vector2.zero);

        // set up the line through the player
        Vector2 startPos = enemy.transform.position;
        Vector2 playerPos = enemy.PlayerTransform.position;

        Vector2 direction = (playerPos - startPos).normalized;
        if (direction == Vector2.zero)
        {
            direction = Vector2.right;
        }

        enemy.TargetPlayerPosition = playerPos;
        Vector2 endPos = playerPos + direction * enemy.TunnelLineLength;
        enemy.TunnelLineTarget = endPos;

        enemy.animator.Play("Burrow BT");

        // start the underground idle timer
        _burrowTimer = burrowIdleTime;
    }

    public override void DoExitLogic()
    {
        base.DoExitLogic();
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (_burrowTimer > 0f)
        {
            _burrowTimer -= Time.deltaTime;
            if (_burrowTimer <= 0f)
            {
                enemy.isTunneling = true;
            }
        }
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
        _burrowTimer = 0f;
    }
}
