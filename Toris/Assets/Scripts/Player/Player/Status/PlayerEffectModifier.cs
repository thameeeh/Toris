using System;

[Serializable]
public struct PlayerEffectModifier
{
    public PlayerEffectType effectType;
    public PlayerEffectModifierMode modifierMode;
    public float numericValue;
    public bool boolValue;

    public PlayerEffectModifier(
        PlayerEffectType effectType,
        PlayerEffectModifierMode modifierMode,
        float numericValue = 0f,
        bool boolValue = false)
    {
        this.effectType = effectType;
        this.modifierMode = modifierMode;
        this.numericValue = numericValue;
        this.boolValue = boolValue;
    }
}
