using UnityEngine;

public sealed class WolfDen : MonoBehaviour, IDamageable
{
    [Header("HP")]
    [SerializeField] private float maxHp = 50f;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private string collapseTrigger = "Collapse";
    [SerializeField] private GameObject activeVisual;
    [SerializeField] private GameObject collapsedVisual;

    private bool cleared;

    private WorldGenRunner runner;
    private Vector2Int denTile;
    private Vector2Int chunkCoord;
    private int spawnId;

    // --- IDamageable ---
    public float MaxHealth { get; set; }
    public float CurrentHealth { get; set; }

    public void Initialize(WorldGenRunner runner, Vector2Int denTile, Vector2Int chunkCoord, int spawnId)
    {
        this.runner = runner;
        this.denTile = denTile;
        this.chunkCoord = chunkCoord;
        this.spawnId = spawnId;

        MaxHealth = maxHp;
        CurrentHealth = MaxHealth;

        cleared = runner.Context.ChunkStates.GetChunkState(chunkCoord).consumedIds.Contains(spawnId);
        ApplyVisualState(cleared);

        if (cleared)
            CurrentHealth = 0f;
    }

    public void Damage(float damageAmount)
    {
        if (cleared) return;

        CurrentHealth -= Mathf.Max(0f, damageAmount);
        if (CurrentHealth <= 0f)
            Die();
    }

    public void Die()
    {
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

        Debug.Log("Den Cleared");
        // later spawner despawn wolves
    }

    private void ApplyVisualState(bool collapsed)
    {
        if (activeVisual != null) activeVisual.SetActive(!collapsed);
        if (collapsedVisual != null) collapsedVisual.SetActive(collapsed);

        if (collapsed && animator != null && !string.IsNullOrEmpty(collapseTrigger))
            animator.SetTrigger(collapseTrigger);
    }
}
