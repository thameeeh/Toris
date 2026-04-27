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
}
