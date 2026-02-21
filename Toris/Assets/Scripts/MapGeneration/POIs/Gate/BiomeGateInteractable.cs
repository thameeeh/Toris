using UnityEngine;

public class BiomeGateInteractable : MonoBehaviour, IInteractable, IPoolable
{
    private WorldGenRunner _runner;
    private Vector2Int _gateTile;

    public void Initialize(WorldGenRunner runner, Vector2Int gateTile)
    {
        _runner = runner;
        _gateTile = gateTile;

        // disable colliders/visuals, reset them here
    }

    public void Interact(GameObject interactor)
    {
        if (_runner == null)
        {
            Debug.LogWarning("GateInteractable: runner not injected.", this);
            return;
        }

        // do VFX/SFX/animation here
        _runner.UseGate(_gateTile);
    }

    // --- Pool lifecycle ---
    public void OnSpawned()
    {
        // reset animator/highlight/prompt when you add them
    }

    public void OnDespawned()
    {
        // Stop any delayed logic
        StopAllCoroutines();

        _runner = null;
        _gateTile = default;
    }
}
