using UnityEngine;

[CreateAssetMenu(fileName = "BloodMage_Idle_Summoned", menuName = "Enemy Logic/Idle Logic/BloodMage Summoned Idle")]
public class BloodMageIdleSO : IdleSOBase<BloodMage>
{
    [SerializeField, Min(0f)] private float summonedSettleDuration = 0.2f;

    private float _settleTimer;

    public bool IsReadyToLeaveIdle { get; private set; }

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();

        enemy.MoveEnemy(Vector2.zero);
        enemy.SetMovementAnimation(false);
        enemy.FacePlayer();
        _settleTimer = summonedSettleDuration;
        IsReadyToLeaveIdle = false;
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (!enemy.HasCombatContext)
        {
            enemy.MoveEnemy(Vector2.zero);
            enemy.SetMovementAnimation(false);
            return;
        }

        if (_settleTimer > 0f)
        {
            _settleTimer -= Time.deltaTime;
            return;
        }

        IsReadyToLeaveIdle = true;
    }

    public override void DoPhysicsLogic()
    {
        base.DoPhysicsLogic();
        enemy.MoveEnemy(Vector2.zero);
    }

    public override void ResetValues()
    {
        base.ResetValues();
        _settleTimer = 0f;
        IsReadyToLeaveIdle = false;
    }

    public void ResetRuntimeState()
    {
        ResetValues();
    }
}
