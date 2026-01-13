public interface IEnemyPool
{
    /// <summary>
    /// Return an active enemy to its pool.
    /// </summary>
    /// <param name="instance">Enemy instance to release.</param>
    void Release(Enemy instance);
}