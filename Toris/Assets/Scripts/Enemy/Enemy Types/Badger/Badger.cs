public class Badger : Enemy
{
    // Badger is on pause until a later ground-up rework.
    // This shell stays so existing prefab, asset, and scene references keep compiling.
    protected override void Start()
    {
        base.Start();
        enabled = false;
    }

    public void StartTunneling()
    {
        // Badger is on pause until a later ground-up rework.
    }

    public void ChangeStateToIdle()
    {
        // Badger is on pause until a later ground-up rework.
    }

    public void BadgerDealDamage()
    {
        // Badger is on pause until a later ground-up rework.
    }

    public void DestroyBadger()
    {
        // Badger is on pause until a later ground-up rework.
    }
}
