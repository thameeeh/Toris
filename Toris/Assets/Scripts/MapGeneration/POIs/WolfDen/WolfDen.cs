using System;
using UnityEngine;

public sealed class WolfDen : MonoBehaviour, IDamageable, IPoolable, IWorldSiteBridge, IWorldSiteActivationListener
{
    [Header("HP")]
    [SerializeField] private float maxHp = 50f;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private string collapseTrigger = "Collapse";
    [SerializeField] private GameObject activeVisual;
    [SerializeField] private GameObject collapsedVisual;

    private bool cleared;

    private IWorldSiteStateService worldSiteStateService;
    private WorldSiteStateHandle worldSiteState;
    private Vector2Int denTile;
    private Vector2Int chunkCoord;
    private int spawnId;

    public bool IsInitialized { get; private set; }
    public bool IsCleared => cleared;
    public Vector3 WorldPosition => transform.position;

    public event Action Initialized;
    public event Action Cleared;
    public event Action<Vector3> DamagedAlert;

    public float MaxHealth { get; set; }
    public float CurrentHealth { get; set; }

    public void Initialize(IWorldSiteStateService worldSiteStateService, Vector2Int denTile, Vector2Int chunkCoord, int spawnId)
    {
        this.worldSiteStateService = worldSiteStateService;
        this.denTile = denTile;
        this.chunkCoord = chunkCoord;
        this.spawnId = spawnId;

        worldSiteState = worldSiteStateService != null
            ? worldSiteStateService.GetSiteState(chunkCoord, spawnId)
            : default;

        foreach (var c in GetComponentsInChildren<Collider2D>(true))
            c.enabled = true;

        MaxHealth = maxHp;
        CurrentHealth = MaxHealth;

        cleared = worldSiteState.IsConsumed;
        ApplyVisualState(cleared);

        if (cleared)
        {
            CurrentHealth = 0f;

            foreach (var c in GetComponentsInChildren<Collider2D>(true))
                c.enabled = false;
        }

        IsInitialized = true;
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
        Initialized = null;
        Cleared = null;
        DamagedAlert = null;
        worldSiteStateService = null;
        worldSiteState = default;

        IsInitialized = false;
        cleared = false;
        CurrentHealth = 0f;
    }
    public void OnSiteActivated()
    {
        if (!IsInitialized)
            return;

        Initialized?.Invoke();
    }
    public void Damage(float damageAmount)
    {
        if (cleared)
            return;

        float appliedDamage = Mathf.Max(0f, damageAmount);
        if (appliedDamage <= 0f)
            return;

        CurrentHealth -= appliedDamage;

        DamagedAlert?.Invoke(WorldPosition);

        if (CurrentHealth <= 0f)
            Die();
    }

    public void Die()
    {
        ClearDen();
    }

    private void ClearDen()
    {
        if (cleared)
            return;

        cleared = true;

        worldSiteState.MarkConsumed();
        ApplyVisualState(true);

        foreach (var c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        Cleared?.Invoke();
    }

    private void ApplyVisualState(bool collapsed)
    {
        if (activeVisual != null) activeVisual.SetActive(!collapsed);
        if (collapsedVisual != null) collapsedVisual.SetActive(collapsed);

        if (collapsed && animator != null && !string.IsNullOrEmpty(collapseTrigger))
            animator.SetTrigger(collapseTrigger);
    }
    public void Initialize(WorldSiteContext siteContext)
    {
        Initialize(
            siteContext.WorldSiteStateService,
            siteContext.Placement.CenterTile,
            siteContext.Placement.ChunkCoord,
            siteContext.SpawnId);
    }
}