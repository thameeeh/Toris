using UnityEngine;

public sealed class WolfEncounterCommandController
{
    private WolfDenEncounterConfig encounterConfig;
    private WorldEncounterOccupantPolicy occupantPolicy;
    private WorldEncounterAlertRuntime alertRuntime;

    public void Configure(
        WolfDenEncounterConfig encounterConfig,
        WorldEncounterOccupantPolicy occupantPolicy,
        WorldEncounterAlertRuntime alertRuntime)
    {
        this.encounterConfig = encounterConfig;
        this.occupantPolicy = occupantPolicy;
        this.alertRuntime = alertRuntime;
    }

    public void Tick(float deltaTime)
    {
        if (!HasConfig())
            return;

        alertRuntime.Tick(
            deltaTime,
            encounterConfig.AlertLevelDecayRate,
            encounterConfig.MaxAlertLevel);
    }

    public void HandleDenDamaged(
        IWorldEncounterSite denSite,
        WorldEncounterServices encounterServices,
        Wolf leader,
        Wolf[] occupants)
    {
        if (!HasConfig())
            return;

        if (denSite == null || denSite.IsCleared)
            return;

        alertRuntime.Raise(
            encounterConfig.AlertLevelPerHit,
            encounterConfig.MaxAlertLevel,
            encounterConfig.AlertLevelDecayDelay);

        if (TryTriggerMaxAlertHowl(leader))
            return;

        Vector3 investigatePoint = BuildInvestigationPoint(denSite, encounterServices);
        float investigateDuration = encounterConfig.DenAlertDuration + alertRuntime.Level;
        float standBonus = alertRuntime.Level * encounterConfig.InvestigateStandBonusPerAlert;

        if (leader != null)
            leader.SetInvestigationTarget(investigatePoint, investigateDuration, standBonus);

        for (int i = 0; i < occupants.Length; i++)
        {
            Wolf occupant = occupants[i];
            if (occupant == null || occupant == leader)
                continue;

            occupant.SetInvestigationTarget(investigatePoint, investigateDuration, standBonus);
        }
    }

    private bool TryTriggerMaxAlertHowl(Wolf leader)
    {
        if (!encounterConfig.HowlAtMaxAlert)
            return false;

        if (!alertRuntime.TryConsumeMaxAlert(encounterConfig.MaxAlertLevel))
            return false;

        if (leader == null)
            return false;

        if (!leader.CanHowl)
            return false;

        if (leader.pack == null)
            return false;

        if (!leader.pack.EnsureLeader(leader))
            return false;

        leader.ClearInvestigationTarget();
        leader.SetAggroStatus(true);
        leader.StateMachine.ChangeState(leader.HowlState);

        alertRuntime.ApplyPostMaxAlertResponse(
            encounterConfig.AlertLevelAfterHowl,
            encounterConfig.MaxAlertLevel,
            encounterConfig.AlertLevelDecayDelay);

        return true;
    }

    private Vector3 BuildInvestigationPoint(
        IWorldEncounterSite denSite,
        WorldEncounterServices encounterServices)
    {
        if (denSite == null)
            return Vector3.zero;

        IWorldNavigationService navigationService = encounterServices != null
            ? encounterServices.NavigationService
            : null;

        if (navigationService == null)
            return denSite.WorldPosition;

        Vector3 denCenterWorld = denSite.WorldPosition;
        Vector2Int denCenterCell = navigationService.WorldToCell(denCenterWorld);
        Transform player = encounterServices != null
            ? encounterServices.PlayerLocator.GetPlayerTransform()
            : null;

        Vector2Int stepDir = GetStepDirectionTowardPlayer(denCenterWorld, player);
        if (stepDir == Vector2Int.zero)
            stepDir = Vector2Int.right;

        Vector2Int currentCell = denCenterCell;
        int maxSteps = Mathf.Max(
            2,
            Mathf.CeilToInt(occupantPolicy.HomeRadius) + encounterConfig.InvestigatePointSearchRadius + 4);

        bool foundBoundaryWalkable = false;

        for (int i = 0; i < maxSteps; i++)
        {
            currentCell += stepDir;

            if (navigationService.IsWalkableCell(currentCell))
            {
                foundBoundaryWalkable = true;
                break;
            }
        }

        if (!foundBoundaryWalkable)
        {
            return FindNearestWalkableCellAround(
                currentCell,
                encounterConfig.InvestigatePointSearchRadius,
                navigationService,
                denCenterWorld);
        }

        int outwardSteps = Mathf.Max(
            1,
            Mathf.RoundToInt(
                encounterConfig.InvestigateBaseStepsFromDen +
                alertRuntime.Level * encounterConfig.InvestigateExtraStepsPerAlert));

        for (int i = 1; i < outwardSteps; i++)
        {
            Vector2Int nextCell = currentCell + stepDir;
            if (!navigationService.IsWalkableCell(nextCell))
                break;

            currentCell = nextCell;
        }

        return navigationService.CellToWorldCenter(currentCell);
    }

    private static Vector2Int GetStepDirectionTowardPlayer(Vector3 denCenterWorld, Transform player)
    {
        if (player == null)
            return Vector2Int.right;

        Vector2 dir = player.position - denCenterWorld;
        if (dir.sqrMagnitude < 0.0001f)
            return Vector2Int.right;

        dir.Normalize();

        int x = Mathf.Abs(dir.x) >= 0.35f ? (dir.x > 0f ? 1 : -1) : 0;
        int y = Mathf.Abs(dir.y) >= 0.35f ? (dir.y > 0f ? 1 : -1) : 0;

        if (x == 0 && y == 0)
        {
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                x = dir.x > 0f ? 1 : -1;
            else
                y = dir.y > 0f ? 1 : -1;
        }

        return new Vector2Int(x, y);
    }

    private Vector3 FindNearestWalkableCellAround(
        Vector2Int startCell,
        int maxTileRadius,
        IWorldNavigationService navigationService,
        Vector3 fallbackWorldPos)
    {
        if (navigationService.IsWalkableCell(startCell))
            return navigationService.CellToWorldCenter(startCell);

        for (int r = 1; r <= maxTileRadius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                Vector2Int top = startCell + new Vector2Int(x, r);
                if (navigationService.IsWalkableCell(top))
                    return navigationService.CellToWorldCenter(top);

                Vector2Int bottom = startCell + new Vector2Int(x, -r);
                if (navigationService.IsWalkableCell(bottom))
                    return navigationService.CellToWorldCenter(bottom);
            }

            for (int y = -r + 1; y <= r - 1; y++)
            {
                Vector2Int right = startCell + new Vector2Int(r, y);
                if (navigationService.IsWalkableCell(right))
                    return navigationService.CellToWorldCenter(right);

                Vector2Int left = startCell + new Vector2Int(-r, y);
                if (navigationService.IsWalkableCell(left))
                    return navigationService.CellToWorldCenter(left);
            }
        }

        return fallbackWorldPos;
    }

    private bool HasConfig()
    {
        return encounterConfig != null && occupantPolicy != null && alertRuntime != null;
    }
}
