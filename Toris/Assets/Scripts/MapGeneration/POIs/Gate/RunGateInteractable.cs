using UnityEngine;

public class RunGateInteractable : MonoBehaviour, IInteractable
{
    public enum Destination
    {
        Town,
        Overworld
    }

    [Header("Run Gate")]
    [SerializeField] Destination destination;

    public void Interact(GameObject interactor)
    {
        var gi = GameInitiator.Instance;
        if (gi == null)
        {
            Debug.LogWarning("RunGateInteractable: GameInitiatior.Instance not found", this);
            return;
        }

        switch (destination)
        {
            case Destination.Town:
                gi.ChangeState(GameInitiator.GameState.InTown);
                break;

            case Destination.Overworld:
                gi.ChangeState(GameInitiator.GameState.InOverworld);
                break;
        }
    }
}