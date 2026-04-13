using System.Collections.Generic;

public interface IPlayerEffectSource
{
    string SourceKey { get; }
    void CollectModifiers(List<PlayerEffectModifier> modifiers);
}
