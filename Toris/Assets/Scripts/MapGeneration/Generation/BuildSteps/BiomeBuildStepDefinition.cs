using UnityEngine;

public abstract class BiomeBuildStepDefinition : ScriptableObject
{
    public abstract void Build(WorldContext ctx);
}