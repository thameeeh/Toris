using UnityEngine;
using UnityEngine.Serialization;

public class RunGateInteractable : MonoBehaviour, IInteractable, IWorldSiteBridge
{
    [Header("Scene Connection")]
    [SerializeField] private string sceneA;
    [SerializeField] private string sceneB;
    [FormerlySerializedAs("sceneTransitionServiceOverride")]
    [SerializeField] private MonoBehaviour runGateTransitionServiceOverride;

    private IRunGateTransitionService runGateTransitionService;

    public void Interact(GameObject interactor)
    {
        runGateTransitionService ??= ResolveRunGateTransitionService();
        if (runGateTransitionService == null)
        {
            Debug.LogWarning("RunGateInteractable: run gate transition service unavailable.", this);
            return;
        }

        runGateTransitionService.UseRunGate(sceneA, sceneB);
    }

    public void Initialize(WorldSiteContext siteContext)
    {
        runGateTransitionService = siteContext.RunGateTransitionService ?? ResolveRunGateTransitionService();
    }

    private IRunGateTransitionService ResolveRunGateTransitionService()
    {
        if (runGateTransitionServiceOverride is IRunGateTransitionService overrideService)
            return overrideService;

        if (SceneTransitionService.Instance != null)
            return SceneTransitionService.Instance;

        SceneTransitionService localSceneTransitionService = FindFirstObjectByType<SceneTransitionService>();
        if (localSceneTransitionService != null)
            return localSceneTransitionService;

        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (runGateTransitionServiceOverride != null && runGateTransitionServiceOverride is not IRunGateTransitionService)
        {
            runGateTransitionServiceOverride = null;
        }
    }
#endif
}
