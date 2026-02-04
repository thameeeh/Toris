using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneEnemyCleanup : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            enemy.RequestDespawn();
        }
    }
}
