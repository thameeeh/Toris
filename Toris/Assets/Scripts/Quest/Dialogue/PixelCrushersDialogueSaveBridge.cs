using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Captures and restores Pixel Crushers dialogue/quest state through the Toris save system.
/// This keeps Pixel Crushers as the dialogue/quest authority while Toris remains the save-file owner.
/// </summary>
public static class PixelCrushersDialogueSaveBridge
{
    private static bool _sceneHookInstalled;
    private static bool _hasPendingSaveData;
    private static string _pendingSaveData;

    public static string CaptureSaveData()
    {
        if (!DialogueManager.hasInstance)
            return string.Empty;

        return PersistentDataManager.GetSaveData();
    }

    public static void RequestApplySaveData(string saveData)
    {
        if (string.IsNullOrWhiteSpace(saveData))
            return;

        EnsureSceneHook();

        _pendingSaveData = saveData;
        _hasPendingSaveData = true;
        TryApplyPendingSaveData();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        _sceneHookInstalled = false;
        _hasPendingSaveData = false;
        _pendingSaveData = string.Empty;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureSceneHook()
    {
        if (_sceneHookInstalled)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        _sceneHookInstalled = true;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryApplyPendingSaveData();
    }

    private static bool TryApplyPendingSaveData()
    {
        if (!_hasPendingSaveData || !DialogueManager.hasInstance)
            return false;

        PersistentDataManager.ApplySaveData(_pendingSaveData);
        DialogueManager.SendUpdateTracker();

        _pendingSaveData = string.Empty;
        _hasPendingSaveData = false;

#if UNITY_EDITOR
        Debug.Log("[PixelCrushersDialogueSaveBridge] Applied pending Pixel Crushers dialogue/quest save data.");
#endif

        return true;
    }
}
