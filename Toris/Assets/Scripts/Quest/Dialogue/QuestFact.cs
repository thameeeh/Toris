using System;

[Serializable]
public struct QuestFact
{
    public QuestFactType Type;
    public string ExactId;
    public string TypeOrTag;
    public int Amount;
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
}
