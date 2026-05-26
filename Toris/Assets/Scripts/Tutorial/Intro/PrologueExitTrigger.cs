using OutlandHaven.Tutorial;
using OutlandHaven.SaveSystem;
using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class PrologueExitTrigger : MonoBehaviour
{
    private const string DefaultTargetSceneName = "MainArea";
    private const string DefaultLoadingMessage = "Entering Safe Haven";

    [SerializeField] private string targetSceneName = DefaultTargetSceneName;
    [SerializeField] private string loadingMessage = DefaultLoadingMessage;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private PrologueStorySequenceController arrivalStorySequence;
    [SerializeField] private GameSessionSO gameSession;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private bool saveOnPrologueCompleted = true;

    private bool _transitionRequested;

    private void OnDisable()
    {
        UnbindArrivalStorySequence();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_transitionRequested || !IsPlayerCollider(other))
            return;

        _transitionRequested = true;

        if (arrivalStorySequence != null)
        {
            arrivalStorySequence.SequenceCompleted += HandleArrivalStoryCompleted;
            if (arrivalStorySequence.TryBegin())
                return;

            UnbindArrivalStorySequence();
        }

        LoadTargetScene();
    }

    private void HandleArrivalStoryCompleted()
    {
        UnbindArrivalStorySequence();
        LoadTargetScene();
    }

    private void UnbindArrivalStorySequence()
    {
        if (arrivalStorySequence != null)
            arrivalStorySequence.SequenceCompleted -= HandleArrivalStoryCompleted;
    }

    private void LoadTargetScene()
    {
        MarkPrologueCompleted();

        string resolvedSceneName = string.IsNullOrWhiteSpace(targetSceneName)
            ? DefaultTargetSceneName
            : targetSceneName.Trim();

        string resolvedLoadingMessage = string.IsNullOrWhiteSpace(loadingMessage)
            ? DefaultLoadingMessage
            : loadingMessage.Trim();

        if (SceneTransitionService.Instance != null)
        {
            SceneTransitionService.Instance.LoadScene(resolvedSceneName, resolvedLoadingMessage);
            return;
        }

        SceneManager.LoadScene(resolvedSceneName);
    }

    private void MarkPrologueCompleted()
    {
        SaveManager resolvedSaveManager = ResolveSaveManager();
        GameSessionSO resolvedSession = resolvedSaveManager != null && resolvedSaveManager.ActiveSession != null
            ? resolvedSaveManager.ActiveSession
            : ResolveGameSession();

        if (resolvedSession == null)
            return;

        // Save-system boundary: the exit trigger only records that the authored
        // prologue gate is complete; scene selection still belongs to Main Menu.
        resolvedSession.MarkPrologueCompleted();

        if (saveOnPrologueCompleted
            && resolvedSaveManager != null
            && resolvedSaveManager.ActiveSession != null)
        {
            resolvedSaveManager.SaveGame(resolvedSaveManager.ActiveSession.ActiveSaveSlot);
        }
    }

    private GameSessionSO ResolveGameSession()
    {
        if (gameSession == null)
            gameSession = GameSessionSO.LoadDefault();

        return gameSession;
    }

    private SaveManager ResolveSaveManager()
    {
        if (saveManager == null)
            saveManager = FindFirstObjectByType<SaveManager>();

        return saveManager;
    }

    private bool IsPlayerCollider(Collider2D other)
    {
        if (other == null)
            return false;

        if (!string.IsNullOrWhiteSpace(playerTag) && other.CompareTag(playerTag))
            return true;

        return other.GetComponentInParent<PlayerInteractor>() != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
            targetSceneName = DefaultTargetSceneName;

        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null)
            triggerCollider.isTrigger = true;
    }
#endif
}
