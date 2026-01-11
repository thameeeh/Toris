using UnityEngine;

public sealed class WolfDen : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private float maxHp = 50f;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private string collapseTrigger = "Collapse";
    [SerializeField] private GameObject activeVisual;
    [SerializeField] private GameObject collapsedVisual;

    private float hp;
    private bool cleared;

    private WorldGenRunner runner;
    private Vector2Int denTile;
    private Vector2Int chunkCoord;
    private int spawnId;

    public void Initialize(WorldGenRunner runner, Vector2Int denTile, Vector2Int chunkCoord, int spawnId)
    {
        this.runner = runner;
        this.denTile = denTile;
        this.chunkCoord = chunkCoord;
        this.spawnId = spawnId;

        hp = maxHp;

        cleared = runner.Context.ChunkStates.GetChunkState(chunkCoord).consumedIds.Contains(spawnId);
        ApplyVisualState(cleared);
    }

    public void ApplyDamage(float amount)
    {
        if (cleared) return;

        hp -= Mathf.Max(0f, amount);
        if (hp <= 0f)
            ClearDen();
    }

    private void ClearDen()
    {
        if (cleared) return;
        cleared = true;

        runner.Context.ChunkStates.MarkConsumed(chunkCoord, spawnId);
        ApplyVisualState(true);

        foreach (var c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        // Later: tell spawner to stop / despawn leader, etc.
    }

    private void ApplyVisualState(bool collapsed)
    {
        if (activeVisual != null) activeVisual.SetActive(!collapsed);
        if (collapsedVisual != null) collapsedVisual.SetActive(collapsed);

        if (collapsed && animator != null && !string.IsNullOrEmpty(collapseTrigger))
            animator.SetTrigger(collapseTrigger);
    }
}
