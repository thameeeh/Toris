using System;

[Serializable]
public struct QuestFact
{
    /// <summary>Category of gameplay event that happened.</summary>
    public QuestFactType Type;
    /// <summary>Stable exact ID for a specific target, such as LeaderWolf or SmithNPC.</summary>
    public string ExactId;
    /// <summary>Optional broader type or tag, such as Wolf, Shopkeeper, or Potion.</summary>
    public string TypeOrTag;
    /// <summary>How much progress this fact contributes.</summary>
    public int Amount;
    /// <summary>Optional context such as MainArea, Plains, Tutorial, or a site ID.</summary>
    public string ContextId;

    public QuestFact(QuestFactType type, string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        Type = type;
        ExactId = string.IsNullOrWhiteSpace(exactId) ? string.Empty : exactId;
        TypeOrTag = string.IsNullOrWhiteSpace(typeOrTag) ? string.Empty : typeOrTag;
        Amount = Math.Max(1, amount);
        ContextId = string.IsNullOrWhiteSpace(contextId) ? string.Empty : contextId;
    }

    public static QuestFact Kill(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.Kill, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact InteractNpc(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.InteractNpc, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact PickUp(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.PickUp, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact EnterScene(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.EnterScene, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact VisitSite(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.VisitSite, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact ClearSite(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.ClearSite, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact Collect(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.Collect, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact Deliver(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.Deliver, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact BuyItem(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.BuyItem, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact SellItem(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.SellItem, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact LevelReached(int level, string contextId = "")
    {
        int safeLevel = Math.Max(1, level);
        return new QuestFact(QuestFactType.LevelReached, $"Level_{safeLevel}", "PlayerLevel", 1, contextId);
    }

    public static QuestFact BiomeReached(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.BiomeReached, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact InteractWorldObject(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.InteractWorldObject, exactId, typeOrTag, amount, contextId);
    }

    public static QuestFact Explore(string exactId, string typeOrTag = "", int amount = 1, string contextId = "")
    {
        return new QuestFact(QuestFactType.Explore, exactId, typeOrTag, amount, contextId);
    }
}
