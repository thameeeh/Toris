public interface IProjectilePool
{
    /// <summary>
    /// Return an active projectile to its pool.
    /// </summary>
    /// <param name="instance">The projectile instance to release.</param>
    void Release(Projectile instance);
}