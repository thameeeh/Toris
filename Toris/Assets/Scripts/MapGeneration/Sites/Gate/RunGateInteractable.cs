using UnityEngine;
using UnityEngine.Serialization;

public class RunGateInteractable : MonoBehaviour, IInteractable, IWorldSiteBridge
{
    [Header("Scene Connection")]
    [SerializeField] private string sceneA;
    [SerializeField] private string sceneB;
    [FormerlySerializedAs("sceneTransitionServiceOverride")]
    [SerializeField] private MonoBehaviour runGateTransitionServiceOverride;
    [SerializeField] private RunStartCheckpointService runStartCheckpointService;

    [Header("Quest & Lock Settings")]
    [SerializeField] private bool _useLockSystem = true;
    [SerializeField] private string _portalUnlockedLuaVariable = "isPortalUnlocked";
    [SerializeField] private string _requiredKeyItemName = "PortalKey";
    [SerializeField] private GameObject _activePortalVisuals;
    [SerializeField] private GameObject _stoneVisuals;
    [SerializeField] private GameObject _gateCollision;
    [SerializeField] private string _unlockSfxId = "world_portal_unlock";
    [SerializeField] private bool _completeQuestEntryOnUnlock = true;
    [SerializeField] private string _questToComplete = "Unlock the Teleporter";
    [SerializeField] private int _entryToComplete = 1;
    [SerializeField] private bool _activateNextEntryOnUnlock = true;
    [SerializeField] private int _entryToActivate = 2;

    [Header("SFX")]
    [SerializeField] private string teleportLeaveSfxId = "world_teleport_leave";
    [SerializeField] private string teleportLoopSfxId = "world_teleport_loop";
    [SerializeField] private Vector3 sfxLocalOffset = Vector3.zero;
    [SerializeField, Range(0f, 2f)] private float sfxVolumeMultiplier = 1f;
    [SerializeField, Min(0f)] private float loopFadeInSeconds = 0.08f;
    [SerializeField, Min(0f)] private float loopFadeOutSeconds = 0.05f;

    private IRunGateTransitionService runGateTransitionService;
    private AudioVoiceHandle teleportLoopHandle;

    private void OnEnable()
    {
        RefreshPortalState();
    }

    private void Start()
    {
        RefreshPortalState();
    }

    private void OnDisable()
    {
        StopTeleportLoop();
    }

    public bool IsUnlocked()
    {
        if (!_useLockSystem) return true;
        if (!PixelCrushers.DialogueSystem.DialogueManager.hasInstance) return false;
        return PixelCrushers.DialogueSystem.DialogueLua.GetVariable(_portalUnlockedLuaVariable).asBool;
    }

    public void RefreshPortalState()
    {
        bool unlocked = IsUnlocked();

        if (_activePortalVisuals != null) _activePortalVisuals.SetActive(unlocked);
        if (_stoneVisuals != null) _stoneVisuals.SetActive(!unlocked);
        
        if (_gateCollision != null)
        {
            // If the gate collision GameObject contains the proximity trigger,
            // we must keep it active so the player can interact to unlock it!
            if (_gateCollision.GetComponent<GateProximity>() != null || _gateCollision.GetComponentInChildren<GateProximity>() != null)
            {
                _gateCollision.SetActive(true);
            }
            else
            {
                // If it is a physical blocker obstacle, disable it when unlocked, enable when locked
                _gateCollision.SetActive(!unlocked);
            }
        }

        if (unlocked)
        {
            TryStartTeleportLoop();
        }
        else
        {
            StopTeleportLoop();
        }
    }

    private bool TryUnlockWithKey()
    {
        OutlandHaven.UIToolkit.GameSessionSO session = OutlandHaven.UIToolkit.GameSessionSO.LoadDefault();
        if (session == null || session.PlayerInventory == null) return false;

        foreach (var slot in session.PlayerInventory.LiveSlots)
        {
            if (!slot.IsEmpty && slot.HeldItem.BaseItem != null && 
                (slot.HeldItem.BaseItem.name == _requiredKeyItemName || slot.HeldItem.BaseItem.ItemName == _requiredKeyItemName))
            {
                bool removed = session.PlayerInventory.RemoveItem(slot.HeldItem, 1);
                if (removed)
                {
                    if (PixelCrushers.DialogueSystem.DialogueManager.hasInstance)
                    {
                        PixelCrushers.DialogueSystem.DialogueLua.SetVariable(_portalUnlockedLuaVariable, true);

                        if (_completeQuestEntryOnUnlock)
                        {
                            // Set Entry 1 to Success
                            PixelCrushersQuestBridge.SetQuestEntryState(_questToComplete, _entryToComplete, "success");
                            
                            // Directly activate Entry 2 in C#
                            if (_activateNextEntryOnUnlock)
                            {
                                PixelCrushersQuestBridge.SetQuestEntryState(_questToComplete, _entryToActivate, "active");
                            }
                            
                            // Automatically transition overall quest to ReturnToNPC so the player goes back to the Guide
                            PixelCrushersQuestBridge.SetQuestState(_questToComplete, "returnToNPC");
                        }
                    }
                    
                    if (!string.IsNullOrWhiteSpace(_unlockSfxId) && AudioBootstrap.Sfx != null)
                    {
                        AudioBootstrap.Sfx.PlayAt(_unlockSfxId, transform.position, SfxPlayRequest.Default);
                    }
                    
                    RefreshPortalState();
                    return true;
                }
            }
        }
        return false;
    }

    public void Interact(GameObject interactor)
    {
        if (_useLockSystem && !IsUnlocked())
        {
            if (TryUnlockWithKey())
            {
#if UNITY_EDITOR
                Debug.Log($"[RunGateInteractable] Portal unlocked using '{_requiredKeyItemName}' key!");
#endif
            }
            else
            {
                Debug.LogWarning($"[RunGateInteractable] Portal is locked. You need the '{_requiredKeyItemName}' key in your inventory to unlock it.");
            }
            return;
        }

        runGateTransitionService ??= ResolveRunGateTransitionService();
        if (runGateTransitionService == null)
        {
            Debug.LogError("[RunGateInteractable] Cannot teleport! SceneTransitionService is missing in the scene. Make sure you started from the bootstrap/MainMenu scene, or that a SceneTransitionService exists in the active scene.", this);
            return;
        }

        // Verify scene setup to prevent silent failures inside SceneTransitionService
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene != sceneA && currentScene != sceneB)
        {
            Debug.LogError($"[RunGateInteractable] Scene Mismatch! The current active scene is '{currentScene}', but the gate is configured with sceneA='{sceneA}' and sceneB='{sceneB}'. The transition service will reject this. Make sure one of them matches the current scene name exactly!", this);
            return;
        }

        // Death respawn uses the MainArea -> ProceduralTiles checkpoint as its reset source.
        runStartCheckpointService ??= FindFirstObjectByType<RunStartCheckpointService>();
        runStartCheckpointService?.CaptureCheckpointIfRunStart(sceneA, sceneB);

        StopTeleportLoop();
        PlayTeleportLeaveSfx();
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

    private void TryStartTeleportLoop()
    {
        if (!Application.isPlaying || teleportLoopHandle.IsValid || AudioBootstrap.Sfx == null || string.IsNullOrWhiteSpace(teleportLoopSfxId))
            return;

        // SFX-only hook: the gate idle loop follows this gate while it is active and does not affect interaction state.
        teleportLoopHandle = AudioBootstrap.Sfx.PlayAttachedLoop(
            teleportLoopSfxId,
            transform,
            sfxLocalOffset,
            MakeSfxRequest(force2D: false, loopFadeInSeconds));
    }

    private void StopTeleportLoop()
    {
        if (!teleportLoopHandle.IsValid || AudioBootstrap.Sfx == null)
            return;

        AudioBootstrap.Sfx.Stop(teleportLoopHandle, loopFadeOutSeconds);
        teleportLoopHandle = AudioVoiceHandle.Invalid;
    }

    private void PlayTeleportLeaveSfx()
    {
        if (AudioBootstrap.Sfx == null || string.IsNullOrWhiteSpace(teleportLeaveSfxId))
            return;

        // SFX-only hook: the leave one-shot plays after the gate accepts interaction and before scene loading starts.
        AudioBootstrap.Sfx.PlayAt(
            teleportLeaveSfxId,
            transform.TransformPoint(sfxLocalOffset),
            MakeSfxRequest(force2D: false));
    }

    private SfxPlayRequest MakeSfxRequest(bool force2D, float fadeInSeconds = 0f)
    {
        SfxPlayRequest request = SfxPlayRequest.Default;
        request.volumeMultiplier = sfxVolumeMultiplier;
        request.fadeInSeconds = fadeInSeconds;
        request.force2D = force2D;
        return request;
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
