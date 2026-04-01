using UnityEngine;

public abstract class SitePlacementRuleDefinition : ScriptableObject
{
    public abstract void BuildSites(WorldContext ctx);
}