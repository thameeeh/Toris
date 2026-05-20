using UnityEngine;

[CreateAssetMenu(fileName = "Boar_Idle_Stand", menuName = "Enemy Logic/Idle Logic/Boar Idle")]
public class BoarIdleSO : IdleSOBase<Boar>
{
    [SerializeField, Min(0f)] private float idleDurationMin = 1.25f;
    [SerializeField, Min(0f)] private float idleDurationMax = 3f;

    private float _idleUntilTime;

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        enemy.StopBoar();
        _idleUntilTime = Time.time + Random.Range(idleDurationMin, Mathf.Max(idleDurationMin, idleDurationMax));
    }

    public override void DoFrameUpdateLogic()
    {
        base.DoFrameUpdateLogic();

        if (enemy.CanStartleCharge)
        {
            enemy.StateMachine.ChangeState(enemy.ChargeState);
            return;
        }

        if (Time.time >= _idleUntilTime)
            enemy.StateMachine.ChangeState(enemy.WanderState);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        idleDurationMin = Mathf.Max(0f, idleDurationMin);
        idleDurationMax = Mathf.Max(idleDurationMin, idleDurationMax);
    }
#endif
}
