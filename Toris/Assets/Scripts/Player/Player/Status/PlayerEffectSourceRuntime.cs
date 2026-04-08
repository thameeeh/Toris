using System.Collections.Generic;

public sealed class StaticPlayerEffectSource : IPlayerEffectSource
{
    private readonly string _sourceKey;
    private PlayerEffectDefinitionSO _effectDefinition;

    public string SourceKey => _sourceKey;
    public PlayerEffectDefinitionSO EffectDefinition => _effectDefinition;

    public StaticPlayerEffectSource(string sourceKey, PlayerEffectDefinitionSO effectDefinition)
    {
        _sourceKey = sourceKey;
        _effectDefinition = effectDefinition;
    }

    public void SetEffectDefinition(PlayerEffectDefinitionSO effectDefinition)
    {
        _effectDefinition = effectDefinition;
    }

    public void CollectModifiers(List<PlayerEffectModifier> modifiers)
    {
        if (modifiers == null || _effectDefinition == null || _effectDefinition.Modifiers == null)
            return;

        IReadOnlyList<PlayerEffectModifier> sourceModifiers = _effectDefinition.Modifiers;
        for (int i = 0; i < sourceModifiers.Count; i++)
        {
            modifiers.Add(sourceModifiers[i]);
        }
    }
}
