using UnityEngine;

public sealed class BoarOasisSite : MonoBehaviour, IPoolable, IWorldSiteContextConsumer, IWorldSiteActivationListener
{
    private const uint BoarCountSalt = 0xB0A201C5u;
    private const float ViewportUnloadPadding = 0.15f;
    private const int SpawnCandidateAttempts = 20;

    private readonly WorldEncounterOccupantCollection<Boar> occupants = new();

    private BoarOasisEncounterConfig encounterConfig;
    private WorldEncounterOccupantPolicy occupantPolicy;
    private WorldEncounterServices encounterServices;
    private int targetBoarCount;
    private float respawnTimer;
    private bool ready;
    private bool releasingSite;

    public void Initialize(WorldSiteContext siteContext)
    {
        encounterConfig = siteContext.GetRuntimeConfig<BoarOasisEncounterConfig>();
        occupantPolicy = encounterConfig != null ? encounterConfig.OccupantPolicy : null;
        encounterServices = siteContext.EncounterServices;
        targetBoarCount = encounterConfig != null
            ? ResolveBoarCount(siteContext, encounterConfig)
            : 0;
        respawnTimer = 0f;
    }

    public void OnSiteActivated()
    {
        ready = HasSpawnConfig();
        if (ready)
            EnsureTargetBoarCount();
    }

    private void Update()
    {
        if (!ready)
            return;

        occupants.RemoveNulls();
        if (occupants.Count >= targetBoarCount)
        {
            respawnTimer = 0f;
            return;
        }

        float respawnDelay = occupantPolicy != null ? occupantPolicy.RespawnDelay : 0f;
        if (respawnDelay <= 0f)
        {
            EnsureTargetBoarCount();
            return;
        }

        respawnTimer += Time.deltaTime;
        if (respawnTimer < respawnDelay)
            return;

        respawnTimer = 0f;
        EnsureTargetBoarCount();
    }

    public void OnSpawned()
    {
        ResetRuntimeState();
    }

    public void OnDespawned()
    {
        ready = false;
        releasingSite = true;

        if (occupantPolicy != null && occupantPolicy.KeepOccupantsOnUnloadIfChasingPlayer)
            DespawnTrackedExceptActiveChasers();
        else
            ForceDespawnAllTracked();

        releasingSite = false;
        encounterConfig = null;
        occupantPolicy = null;
        encounterServices = null;
        targetBoarCount = 0;
        respawnTimer = 0f;
    }

    private void ResetRuntimeState()
    {
        ready = false;
        releasingSite = false;
        targetBoarCount = 0;
        respawnTimer = 0f;
        occupants.Clear();
    }

    private bool HasSpawnConfig()
    {
        return encounterConfig != null
               && encounterConfig.BoarPrefab != null
               && occupantPolicy != null
               && encounterServices != null
               && encounterServices.EnemySpawnService != null
               && targetBoarCount > 0;
    }

    private static int ResolveBoarCount(
        WorldSiteContext siteContext,
        BoarOasisEncounterConfig config)
    {
        int minCount = Mathf.Max(0, config.MinBoarCount);
        int maxCount = Mathf.Max(minCount, config.MaxBoarCount);
        if (maxCount == minCount)
            return minCount;

        uint countHash = DeterministicHash.Hash(
            (uint)Mathf.Max(0, siteContext.SpawnId),
            siteContext.Placement.CenterTile.x,
            siteContext.Placement.CenterTile.y,
            BoarCountSalt);

        int range = maxCount - minCount + 1;
        int offset = Mathf.FloorToInt(DeterministicHash.Hash01(countHash) * range);
        return Mathf.Clamp(minCount + offset, minCount, maxCount);
    }

    private void EnsureTargetBoarCount()
    {
        if (!HasSpawnConfig())
            return;

        occupants.RemoveNulls();
        int missingCount = targetBoarCount - occupants.Count;
        for (int i = 0; i < missingCount; i++)
            SpawnBoar();
    }

    private void SpawnBoar()
    {
        Vector3 spawnPosition = PickWalkableSpawnPosition();
        Enemy enemy = encounterServices.EnemySpawnService.SpawnEnemy(
            encounterConfig.BoarPrefab,
            spawnPosition,
            Quaternion.identity);

        Boar boar = enemy as Boar;
        if (boar == null)
        {
            enemy?.RequestDespawn();
            return;
        }

        ConfigureHomeAnchor(boar);
        occupants.Track(boar, OnBoarReleased);
    }

    private Vector3 PickWalkableSpawnPosition()
    {
        float spawnRadius = occupantPolicy != null ? Mathf.Max(0f, occupantPolicy.SpawnRadius) : 0f;
        Vector3 sitePosition = transform.position;

        for (int i = 0; i < SpawnCandidateAttempts; i++)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 candidate = sitePosition + new Vector3(offset.x, offset.y, 0f);
            if (TryResolveWalkableCell(candidate, out Vector3 resolvedPosition))
                return resolvedPosition;
        }

        return FindNearestWalkableSpawn(sitePosition, Mathf.CeilToInt(spawnRadius));
    }

    private bool TryResolveWalkableCell(Vector3 candidateWorldPosition, out Vector3 resolvedPosition)
    {
        IWorldNavigationService navigationService = encounterServices != null
            ? encounterServices.NavigationService
            : null;

        if (navigationService == null)
        {
            resolvedPosition = candidateWorldPosition;
            return true;
        }

        Vector2Int cell = navigationService.WorldToCell(candidateWorldPosition);
        if (!navigationService.IsWalkableCell(cell))
        {
            resolvedPosition = default;
            return false;
        }

        resolvedPosition = navigationService.CellToWorldCenter(cell);
        return true;
    }

    private Vector3 FindNearestWalkableSpawn(Vector3 desiredWorldPosition, int maxTileRadius)
    {
        IWorldNavigationService navigationService = encounterServices != null
            ? encounterServices.NavigationService
            : null;

        if (navigationService == null)
            return desiredWorldPosition;

        Vector2Int startCell = navigationService.WorldToCell(desiredWorldPosition);
        if (navigationService.IsWalkableCell(startCell))
            return navigationService.CellToWorldCenter(startCell);

        int resolvedMaxTileRadius = Mathf.Max(1, maxTileRadius);
        for (int radius = 1; radius <= resolvedMaxTileRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                Vector2Int top = startCell + new Vector2Int(x, radius);
                if (navigationService.IsWalkableCell(top))
                    return navigationService.CellToWorldCenter(top);

                Vector2Int bottom = startCell + new Vector2Int(x, -radius);
                if (navigationService.IsWalkableCell(bottom))
                    return navigationService.CellToWorldCenter(bottom);
            }

            for (int y = -radius + 1; y <= radius - 1; y++)
            {
                Vector2Int right = startCell + new Vector2Int(radius, y);
                if (navigationService.IsWalkableCell(right))
                    return navigationService.CellToWorldCenter(right);

                Vector2Int left = startCell + new Vector2Int(-radius, y);
                if (navigationService.IsWalkableCell(left))
                    return navigationService.CellToWorldCenter(left);
            }
        }

        return desiredWorldPosition;
    }

    private void ConfigureHomeAnchor(Boar boar)
    {
        if (boar == null || occupantPolicy == null)
            return;

        if (!boar.TryGetComponent(out HomeAnchor homeAnchor))
            homeAnchor = boar.gameObject.AddComponent<HomeAnchor>();

        homeAnchor.Center = transform.position;
        homeAnchor.Radius = occupantPolicy.HomeRadius;
        boar.RefreshHomeAnchor();
    }

    private void OnBoarReleased(Boar boar)
    {
        if (releasingSite)
            return;

        respawnTimer = 0f;
    }

    private void DespawnTrackedExceptActiveChasers()
    {
        Transform player = FindPlayerTransform();
        occupants.ReleaseWhere(
            keepAlivePredicate: boar => ShouldKeepAliveOnUnload(boar, player),
            detachAction: DetachFromOasis,
            releaseAction: boar => boar.RequestDespawn());
    }

    private void ForceDespawnAllTracked()
    {
        occupants.ReleaseAll(boar => boar.RequestDespawn());
    }

    private Transform FindPlayerTransform()
    {
        return encounterServices != null && encounterServices.PlayerLocator != null
            ? encounterServices.PlayerLocator.GetPlayerTransform()
            : null;
    }

    private bool ShouldKeepAliveOnUnload(Boar boar, Transform player)
    {
        if (boar == null || occupantPolicy == null)
            return false;

        if (IsVisibleForUnload(boar))
            return true;

        if (player == null)
            return false;

        float maxDistance = occupantPolicy.KeepChaseIfWithinPlayerRange;
        float maxDistanceSqr = maxDistance * maxDistance;
        Vector3 offset = boar.transform.position - player.position;
        if (offset.sqrMagnitude > maxDistanceSqr)
            return false;

        return boar.IsAggroed || boar.HasActiveCombatTarget;
    }

    private static bool IsVisibleForUnload(Boar boar)
    {
        if (boar == null)
            return false;

        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector3 viewportPosition = camera.WorldToViewportPoint(boar.transform.position);
            return viewportPosition.z > 0f
                   && viewportPosition.x >= -ViewportUnloadPadding
                   && viewportPosition.x <= 1f + ViewportUnloadPadding
                   && viewportPosition.y >= -ViewportUnloadPadding
                   && viewportPosition.y <= 1f + ViewportUnloadPadding;
        }

        Renderer[] renderers = boar.GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null && renderer.isVisible)
                return true;
        }

        return false;
    }

    private void DetachFromOasis(Boar boar)
    {
        if (boar == null)
            return;

        if (boar.TryGetComponent(out HomeAnchor homeAnchor))
        {
            homeAnchor.Center = boar.transform.position;
            if (occupantPolicy != null)
                homeAnchor.Radius = occupantPolicy.HomeRadius;

            boar.RefreshHomeAnchor();
        }
    }
}
