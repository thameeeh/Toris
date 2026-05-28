using UnityEngine;

/// <summary>
/// Bridges a WorldItem collectible with the world site persistence system.
/// When placed on a procedurally spawned prefab:
/// 1. Receives the WorldSiteStateHandle from WorldSiteActivationPipeline
/// 2. Listens for the WorldItem being destroyed (collected)
/// 3. Marks the site as consumed so it never respawns
/// </summary>
public class CollectibleSiteBridge : MonoBehaviour, IWorldSiteContextConsumer
{
    private WorldSiteStateHandle _siteState;
    private bool _initialized;
    private bool _wasCollected;

    public void Initialize(WorldSiteContext siteContext)
    {
        // The pipeline calls this with the site's unique state handle
        _siteState = siteContext.WorldSiteStateService != null
            ? siteContext.WorldSiteStateService.GetSiteState(
                siteContext.Placement.ChunkCoord,
                siteContext.SpawnId)
            : default;

        _initialized = _siteState.IsValid;
    }

    /// <summary>
    /// Call this when the collectible is picked up.
    /// Marks the spawn point as consumed in ChunkStateStore.
    /// </summary>
    public void OnCollected()
    {
        _wasCollected = true;
        if (_initialized)
        {
            _siteState.MarkConsumed();
        }
    }

    private void OnDestroy()
    {
        // Only persist if actually collected, not on chunk unload
        if (_wasCollected && _initialized)
        {
            _siteState.MarkConsumed();
        }
    }
}
