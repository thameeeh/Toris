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
    /// <summary>The player bought an item.</summary>
    BuyItem = 7,
    /// <summary>The player sold an item.</summary>
    SellItem = 8,
    /// <summary>The player reached a certain level.</summary>
    LevelReached = 9,
    /// <summary>The player entered a biome.</summary>
    EnterBiome = 10,
    /// <summary>The player interacted with a world object.</summary>
    InteractWorldObject = 11
}
