using UnityEngine;

[CreateAssetMenu(fileName = "Deer_Dead", menuName = "Outland Haven/Enemy/Behaviors/Dead Logic/Deer Dead")]
public class DeerDeadSO : DeadSOBase<Deer>
{
    [SerializeField, Min(0f)] private float holdDuration = 0.15f;
    [SerializeField, Min(0f)] private float fadeDuration = 0.35f;
    [SerializeField, Min(0f)] private float despawnDelay = 0.1f;

    public override void DoEnterLogic()
    {
        base.DoEnterLogic();
        enemy.BeginFallbackDeath(holdDuration, fadeDuration, despawnDelay);
    }

    public override void DoExitLogic()
    {
        enemy.StopFallbackDeath();
        base.DoExitLogic();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        holdDuration = Mathf.Max(0f, holdDuration);
        fadeDuration = Mathf.Max(0f, fadeDuration);
        despawnDelay = Mathf.Max(0f, despawnDelay);
    }
#endif
}
