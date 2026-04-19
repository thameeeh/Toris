using UnityEngine;

public sealed class NecromancerGraveEncounterRelay : MonoBehaviour
{
    private Enemy boundEnemy;
    private WorldSiteStateHandle worldSiteState;
    private string activeStateKey = string.Empty;
    private bool isBound;
    private bool wasConsumed;

    private void Awake()
    {
        if (boundEnemy == null)
            TryGetComponent(out boundEnemy);
    }

    public void Bind(WorldSiteStateHandle siteState, string encounterActiveStateKey)
    {
        if (boundEnemy == null)
            TryGetComponent(out boundEnemy);

        Unbind();

        if (boundEnemy == null)
            return;

        worldSiteState = siteState;
        activeStateKey = encounterActiveStateKey;
        wasConsumed = siteState.IsConsumed;

        boundEnemy.Died += HandleEnemyDied;
        boundEnemy.Despawned += HandleEnemyDespawned;
        isBound = true;
    }

    private void HandleEnemyDied(Enemy enemy)
    {
        if (enemy != boundEnemy)
            return;

        wasConsumed = true;
        worldSiteState.MarkConsumed();
        worldSiteState.SetBool(activeStateKey, false);
    }

    private void HandleEnemyDespawned(Enemy enemy)
    {
        if (enemy != boundEnemy)
            return;

        if (!wasConsumed)
            worldSiteState.SetBool(activeStateKey, false);

        Unbind();
    }

    private void Unbind()
    {
        if (!isBound)
            return;

        if (boundEnemy != null)
        {
            boundEnemy.Died -= HandleEnemyDied;
            boundEnemy.Despawned -= HandleEnemyDespawned;
        }

        worldSiteState = default;
        activeStateKey = string.Empty;
        wasConsumed = false;
        isBound = false;
    }
}
