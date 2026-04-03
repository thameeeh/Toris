using UnityEngine;

public class BiomeGateInteractable : MonoBehaviour, IInteractable, IPoolable, IWorldSiteBridge
{
    private IGateTransitionService gateTransitionService;
    private Vector2Int gateTile;

    public void Initialize(IGateTransitionService gateTransitionService, Vector2Int gateTile)
    {
        this.gateTransitionService = gateTransitionService;
        this.gateTile = gateTile;

        // disable colliders/visuals, reset them here
    }

    public void Interact(GameObject interactor)
    {
        if (gateTransitionService == null)
        {
            Debug.LogWarning("GateInteractable: gate transition service not injected.", this);
            return;
        }

        gateTransitionService.UseGate(gateTile);
    }

    public void OnSpawned()
    {
        // reset animator/highlight/prompt when you add them
    }

    public void OnDespawned()
    {
        StopAllCoroutines();

        gateTransitionService = null;
        gateTile = default;
    }

    public void Initialize(WorldSiteContext siteContext)
    {
        Initialize(siteContext.GateTransitionService, siteContext.Placement.CenterTile);
    }
}
