using System;
using System.Collections.Generic;
using OutlandHaven.Inventory;

// Death screen related: runtime payload shown by the UI before the respawn penalty is applied.
[Serializable]
public sealed class DeathPenaltySummary
{
    private readonly List<DeathItemLossSummary> _lostItems;

    public DeathPenaltySummary(
        float experienceLost,
        int goldLost,
        int backpackItemsLost,
        int potionItemsLost,
        string causeOfDeath,
        IEnumerable<DeathItemLossSummary> lostItems,
        string deathMessage = null)
    {
        ExperienceLost = experienceLost < 0f ? 0f : experienceLost;
        GoldLost = Math.Max(0, goldLost);
        BackpackItemsLost = Math.Max(0, backpackItemsLost);
        PotionItemsLost = Math.Max(0, potionItemsLost);
        CauseOfDeath = string.IsNullOrWhiteSpace(causeOfDeath)
            ? DeathCauseSnapshot.UnknownDisplayName
            : causeOfDeath.Trim();
        DeathMessage = string.IsNullOrWhiteSpace(deathMessage)
            ? DeathCauseMessageFormatter.DefaultSubtitle
            : deathMessage.Trim();
        _lostItems = lostItems != null
            ? new List<DeathItemLossSummary>(lostItems)
            : new List<DeathItemLossSummary>();
    }

    public float ExperienceLost { get; }
    public int GoldLost { get; }
    public int BackpackItemsLost { get; }
    public int PotionItemsLost { get; }
    public string CauseOfDeath { get; }
    public string DeathMessage { get; }
    public int TotalItemsLost => BackpackItemsLost + PotionItemsLost;
    public IReadOnlyList<DeathItemLossSummary> LostItems => _lostItems;
}

[Serializable]
public sealed class DeathItemLossSummary
{
    public DeathItemLossSummary(
        string itemId,
        string displayName,
        int count,
        DeathPenaltyInventorySource source,
        InventoryItemSO itemBlueprint = null)
    {
        ItemId = itemId;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? itemId ?? "Unknown Item" : displayName;
        Count = Math.Max(0, count);
        Source = source;
        ItemBlueprint = itemBlueprint;
    }

    public string ItemId { get; }
    public string DisplayName { get; }
    public int Count { get; }
    public DeathPenaltyInventorySource Source { get; }
    public InventoryItemSO ItemBlueprint { get; }
}

public enum DeathPenaltyInventorySource
{
    Backpack,
    Potion
}
