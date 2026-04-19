public interface IVisualPool
{
    /// <summary>
    /// Return an active pooled visual to its pool.
    /// </summary>
    /// <param name="instance">The pooled visual instance to release.</param>
    void Release(PooledVisualInstance instance);
}
