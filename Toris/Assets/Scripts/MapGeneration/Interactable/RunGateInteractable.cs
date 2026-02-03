using UnityEngine;
using UnityEngine.SceneManagement;

public class RunGateInteractable : MonoBehaviour, IInteractable
{
    [Header("Scene Connection")]
    [SerializeField] private string sceneA;
    [SerializeField] private string sceneB;

    public void Interact(GameObject interactor)
    {
        var svc = SceneTransitionService.Instance;
        if (svc == null)
        {
            Debug.LogError("RunGateInteractable: SceneTransitionService missing.");
            return;
        }

        string current = SceneManager.GetActiveScene().name;

        if (current == sceneA)
        {
            svc.LoadScene(sceneB);
        }
        else if (current == sceneB)
        {
            svc.LoadScene(sceneA);
        }
        else
        {
            Debug.LogWarning(
                $"RunGateInteractable: Current scene '{current}' " +
                $"does not match '{sceneA}' or '{sceneB}'.",
                this
            );
        }
    }
}
