using UnityEngine;

public class GateInteractable : MonoBehaviour, IInteractable
{
    private WorldGenRunner _runner;
    private Vector2Int _gateTile;

    public void Initialize(WorldGenRunner runner, Vector2Int gateTile)
    {
        _runner = runner;
        _gateTile = gateTile;
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
}
