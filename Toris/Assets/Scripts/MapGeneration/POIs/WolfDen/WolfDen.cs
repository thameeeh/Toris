using System;
using UnityEngine;

public sealed class WolfDen : MonoBehaviour, IDamageable, IPoolable, IWorldSiteBridge, IWorldSiteActivationListener, IWorldEncounterSite
{
    private const string ActiveVisualChildName = "ActiveVisual";
    private const string CollapsedVisualChildName = "CollapsedVisual";

    [Header("HP")]
    [SerializeField] private float maxHp = 50f;

    [Header("Visuals")]
    [SerializeField] private Animator animator;
    [SerializeField] private string collapseTrigger = "Collapse";
    [SerializeField] private GameObject activeVisual;
    [SerializeField] private GameObject collapsedVisual;

    private Collider2D[] cachedColliders;
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

    private void Awake()
    {
        cachedColliders = GetComponentsInChildren<Collider2D>(true);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        activeVisual = ResolveChildVisual(activeVisual, ActiveVisualChildName);
        collapsedVisual = ResolveChildVisual(collapsedVisual, CollapsedVisualChildName);
    }

    private GameObject ResolveChildVisual(GameObject currentVisual, string childName)
    {
        if (currentVisual != null && currentVisual.transform != null && currentVisual.transform.IsChildOf(transform))
            return currentVisual;

        Transform child = transform.Find(childName);
        if (child != null)
            return child.gameObject;

        return currentVisual;
    }
#endif

    public void Initialize(IWorldSiteStateService worldSiteStateService, Vector2Int denTile, Vector2Int chunkCoord, int spawnId)
    {
        this.worldSiteStateService = worldSiteStateService;
        this.denTile = denTile;
        this.chunkCoord = chunkCoord;
        this.spawnId = spawnId;

        worldSiteState = worldSiteStateService != null
            ? worldSiteStateService.GetSiteState(chunkCoord, spawnId)
            : default;

        SetCollidersEnabled(true);
        MaxHealth = maxHp;
        CurrentHealth = MaxHealth;

        cleared = worldSiteState.IsConsumed;
        ApplyVisualState(cleared, playCollapseAnimation: false);

        if (cleared)
        {
            CurrentHealth = 0f;
            SetCollidersEnabled(false);
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

        HideVisuals();
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

        HideVisuals();
        SetCollidersEnabled(true);
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
        ApplyVisualState(true, playCollapseAnimation: true);

        SetCollidersEnabled(false);
        Cleared?.Invoke();
    }

    private void ApplyVisualState(bool collapsed, bool playCollapseAnimation)
    {
        if (activeVisual != null) activeVisual.SetActive(!collapsed);
        if (collapsedVisual != null) collapsedVisual.SetActive(collapsed);

        if (playCollapseAnimation && collapsed && animator != null && !string.IsNullOrEmpty(collapseTrigger))
            animator.SetTrigger(collapseTrigger);
    }

    private void HideVisuals()
    {
        if (activeVisual != null)
            activeVisual.SetActive(false);

        if (collapsedVisual != null)
            collapsedVisual.SetActive(false);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (cachedColliders == null)
            return;

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            Collider2D collider = cachedColliders[i];
            if (collider != null)
                collider.enabled = enabled;
        }
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
