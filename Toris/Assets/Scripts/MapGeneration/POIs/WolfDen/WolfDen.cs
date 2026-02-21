using UnityEngine;

public sealed class WolfDen : MonoBehaviour, IDamageable, IPoolable
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

    public bool IsInitialized { get; private set; }
    public bool IsCleared => cleared;
    public Vector3 WorldPosition => transform.position;

    // --- IDamageable ---
    public float MaxHealth { get; set; }
    public float CurrentHealth { get; set; }

    public void Initialize(WorldGenRunner runner, Vector2Int denTile, Vector2Int chunkCoord, int spawnId)
    {
        this.runner = runner;
        this.denTile = denTile;
        this.chunkCoord = chunkCoord;
        this.spawnId = spawnId;

        foreach (var c in GetComponentsInChildren<Collider2D>(true))
            c.enabled = true;

        MaxHealth = maxHp;
        CurrentHealth = MaxHealth;

        cleared = runner.Context.ChunkStates.GetChunkState(chunkCoord).consumedIds.Contains(spawnId);
        ApplyVisualState(cleared);

        if (cleared)
        {
            CurrentHealth = 0f;

            foreach (var c in GetComponentsInChildren<Collider2D>(true))
                c.enabled = false;
        }

        IsInitialized = true;

        var spawner = GetComponent<WolfDenSpawner>();
        if (spawner != null)
            spawner.OnDenInitialized();

    }

    public void OnSpawned()
    {
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    public void OnDespawned()
    {
        // stop particles/coroutines/etc later
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

        // despawn here
        var spawner = GetComponent<WolfDenSpawner>();
        if (spawner != null)
            spawner.OnDenCleared();
    }

    private void ApplyVisualState(bool collapsed)
    {
        if (activeVisual != null) activeVisual.SetActive(!collapsed);
        if (collapsedVisual != null) collapsedVisual.SetActive(collapsed);

        if (collapsed && animator != null && !string.IsNullOrEmpty(collapseTrigger))
            animator.SetTrigger(collapseTrigger);
    }
}
