using UnityEngine;

/// <summary>
/// Simple bootstrapper that ensures a GameplayPoolManager exists and is configured
/// before gameplay systems request pooled spawns.
/// </summary>
public class GameplayPoolBootstrap : MonoBehaviour
{
    [SerializeField] private GameplayPoolConfiguration configuration;
    [SerializeField] private Transform projectileRoot;
    [SerializeField] private Transform enemyRoot;

    private void Start()
    {
        var manager = GameplayPoolManager.Instance;

        if (manager == null)
        {
            manager = gameObject.AddComponent<GameplayPoolManager>();
        }

        manager.Configure(configuration, projectileRoot, enemyRoot);
    }
}