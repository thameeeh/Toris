using UnityEngine;
using UnityEngine.SceneManagement;

public class RunGateInteractable : MonoBehaviour, IInteractable, IWorldSiteBridge
{
    [Header("Scene Connection")]
    [SerializeField] private string sceneA;
    [SerializeField] private string sceneB;
    [SerializeField] private SceneTransitionService sceneTransitionServiceOverride;

    private ISceneTransitionService sceneTransitionService;

    private void Awake()
    {
        sceneTransitionService = ResolveSceneTransitionService();
    }

    public void Interact(GameObject interactor)
    {
        sceneTransitionService ??= ResolveSceneTransitionService();
        if (sceneTransitionService == null)
        {
            Debug.LogWarning("RunGateInteractable: scene transition service unavailable.", this);
            return;
        }

        string current = SceneManager.GetActiveScene().name;

        if (current == sceneA)
        {
            sceneTransitionService.LoadScene(sceneB);
        }
        else if (current == sceneB)
        {
            sceneTransitionService.LoadScene(sceneA);
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
    public void Initialize(WorldSiteContext siteContext)
    {
        sceneTransitionService = siteContext.SceneTransitionService ?? ResolveSceneTransitionService();
    }

    private ISceneTransitionService ResolveSceneTransitionService()
    {
        if (sceneTransitionServiceOverride != null)
            return sceneTransitionServiceOverride;

        if (SceneTransitionService.Instance != null)
            return SceneTransitionService.Instance;

        return FindFirstObjectByType<SceneTransitionService>();
    }
}
