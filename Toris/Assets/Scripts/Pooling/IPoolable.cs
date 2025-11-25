public interface IPoolable
{
    /// <summary>Called when an instance is fetched from its pool.</summary>
    void OnSpawned();

    /// <summary>Called when an instance is returned to its pool.</summary>
    void OnDespawned();
}