public enum QuestFactType
{
    /// <summary>No fact. Reporters ignore this.</summary>
    None = 0,
    /// <summary>An enemy or target was killed.</summary>
    Kill = 1,
    /// <summary>An item was picked up.</summary>
    PickUp = 2,
    /// <summary>The player entered a scene or major area.</summary>
    EnterScene = 3,
    /// <summary>The player visited a site or trigger area.</summary>
    VisitSite = 4,
    /// <summary>A site, encounter, or authored objective was cleared.</summary>
    ClearSite = 5,
    /// <summary>The player interacted with an NPC.</summary>
    InteractNpc = 6,
    /// <summary>An item or authored collectible was collected.</summary>
    Collect = 7,
    /// <summary>An item, message, or authored objective was delivered.</summary>
    Deliver = 8,
    /// <summary>The player bought an item from a shop.</summary>
    BuyItem = 9,
    /// <summary>The player sold an item to a shop.</summary>
    SellItem = 10,
    /// <summary>The player reached a character level.</summary>
    LevelReached = 11,
    /// <summary>The player reached or entered a biome.</summary>
    BiomeReached = 12,
    /// <summary>The player interacted with a non-NPC world object.</summary>
    InteractWorldObject = 13,
    /// <summary>The player explored a named area or authored exploration point.</summary>
    Explore = 14
}
