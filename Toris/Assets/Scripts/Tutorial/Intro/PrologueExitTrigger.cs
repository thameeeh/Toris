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

    private bool _transitionRequested;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_transitionRequested || !IsPlayerCollider(other))
            return;

        _transitionRequested = true;
        LoadTargetScene();
    }

    private void LoadTargetScene()
    {
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
