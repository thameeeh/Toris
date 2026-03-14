using System.Collections.Generic;

public interface IPlayerEffectModifierSource
{
    void GetEffectModifiers(List<PlayerEffectModifier> modifiers);
}