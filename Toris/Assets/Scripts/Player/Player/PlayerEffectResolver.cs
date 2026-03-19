using System.Collections.Generic;
using UnityEngine;

public static class PlayerEffectResolver
{
    public static PlayerResolvedEffects Resolve(
        PlayerBaseEffectsSO baseEffects,
        IReadOnlyList<PlayerEffectModifier> modifiers)
    {
        PlayerResolvedEffects resolvedEffects = CreateFromBase(baseEffects);

        float maxHealthAdditive = 0f;
        float maxHealthMultiplicative = 1f;

        float maxStaminaAdditive = 0f;
        float maxStaminaMultiplicative = 1f;

        float staminaRegenAdditive = 0f;
        float staminaRegenMultiplicative = 1f;

        float moveSpeedMultiplicative = 1f;
        float dashSpeedMultiplicative = 1f;
        float outgoingDamageMultiplicative = 1f;
        float incomingDamageMultiplicative = 1f;

        bool poisonImmunity = resolvedEffects.isPoisonImmune;
        bool burningImmunity = resolvedEffects.isBurningImmune;
        bool bleedingImmunity = resolvedEffects.isBleedingImmune;

        float strengthBonusAdditive = 0f;
        float strengthBonusMultiplicative = 1f;

        float defenceBonusAdditive = 0f;
        float defenceBonusMultiplicative = 1f;

        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                PlayerEffectModifier modifier = modifiers[i];

                switch (modifier.effectType)
                {
                    case PlayerEffectType.MaxHealth:
                        ApplyNumericModifier(modifier, ref maxHealthAdditive, ref maxHealthMultiplicative);
                        break;

                    case PlayerEffectType.MaxStamina:
                        ApplyNumericModifier(modifier, ref maxStaminaAdditive, ref maxStaminaMultiplicative);
                        break;

                    case PlayerEffectType.StaminaRegenPerSecond:
                        ApplyNumericModifier(modifier, ref staminaRegenAdditive, ref staminaRegenMultiplicative);
                        break;

                    case PlayerEffectType.MoveSpeedMultiplier:
                        ApplyMultiplierOnlyModifier(modifier, ref moveSpeedMultiplicative);
                        break;

                    case PlayerEffectType.DashSpeedMultiplier:
                        ApplyMultiplierOnlyModifier(modifier, ref dashSpeedMultiplicative);
                        break;

                    case PlayerEffectType.OutgoingDamageMultiplier:
                        ApplyMultiplierOnlyModifier(modifier, ref outgoingDamageMultiplicative);
                        break;

                    case PlayerEffectType.IncomingDamageMultiplier:
                        ApplyMultiplierOnlyModifier(modifier, ref incomingDamageMultiplicative);
                        break;

                    case PlayerEffectType.PoisonImmunity:
                        ApplyBooleanModifier(modifier, ref poisonImmunity);
                        break;

                    case PlayerEffectType.BurningImmunity:
                        ApplyBooleanModifier(modifier, ref burningImmunity);
                        break;

                    case PlayerEffectType.BleedingImmunity:
                        ApplyBooleanModifier(modifier, ref bleedingImmunity);
                        break;

                    case PlayerEffectType.StrengthBonus:
                        ApplyNumericModifier(modifier, ref strengthBonusAdditive, ref strengthBonusMultiplicative);
                        break;

                    case PlayerEffectType.DefenceBonus:
                        ApplyNumericModifier(modifier, ref defenceBonusAdditive, ref defenceBonusMultiplicative);
                        break;
                }
            }
        }

        resolvedEffects.maxHealth = Mathf.Max(1f, (resolvedEffects.maxHealth + maxHealthAdditive) * maxHealthMultiplicative);
        resolvedEffects.maxStamina = Mathf.Max(0f, (resolvedEffects.maxStamina + maxStaminaAdditive) * maxStaminaMultiplicative);
        resolvedEffects.staminaRegenPerSecond = Mathf.Max(0f, (resolvedEffects.staminaRegenPerSecond + staminaRegenAdditive) * staminaRegenMultiplicative);

        resolvedEffects.moveSpeedMultiplier = Mathf.Max(0f, resolvedEffects.moveSpeedMultiplier * moveSpeedMultiplicative);
        resolvedEffects.dashSpeedMultiplier = Mathf.Max(0f, resolvedEffects.dashSpeedMultiplier * dashSpeedMultiplicative);
        resolvedEffects.outgoingDamageMultiplier = Mathf.Max(0f, resolvedEffects.outgoingDamageMultiplier * outgoingDamageMultiplicative);
        resolvedEffects.incomingDamageMultiplier = Mathf.Max(0f, resolvedEffects.incomingDamageMultiplier * incomingDamageMultiplicative);

        resolvedEffects.isPoisonImmune = poisonImmunity;
        resolvedEffects.isBurningImmune = burningImmunity;
        resolvedEffects.isBleedingImmune = bleedingImmunity;

        resolvedEffects.strengthBonus = (resolvedEffects.strengthBonus + strengthBonusAdditive) * strengthBonusMultiplicative;
        resolvedEffects.defenceBonus = (resolvedEffects.defenceBonus + defenceBonusAdditive) * defenceBonusMultiplicative;

        return resolvedEffects;
    }

    public static PlayerResolvedEffects CreateFromBase(PlayerBaseEffectsSO baseEffects)
    {
        if (baseEffects == null)
            return PlayerResolvedEffects.CreateDefault();

        return new PlayerResolvedEffects
        {
            maxHealth = Mathf.Max(1f, baseEffects.maxHealth),
            maxStamina = Mathf.Max(0f, baseEffects.maxStamina),
            staminaRegenPerSecond = Mathf.Max(0f, baseEffects.staminaRegenPerSecond),

            moveSpeedMultiplier = Mathf.Max(0f, baseEffects.moveSpeedMultiplier),
            dashSpeedMultiplier = Mathf.Max(0f, baseEffects.dashSpeedMultiplier),

            outgoingDamageMultiplier = Mathf.Max(0f, baseEffects.outgoingDamageMultiplier),
            incomingDamageMultiplier = Mathf.Max(0f, baseEffects.incomingDamageMultiplier),

            isPoisonImmune = baseEffects.isPoisonImmune,
            isBurningImmune = baseEffects.isBurningImmune,
            isBleedingImmune = baseEffects.isBleedingImmune,

            strengthBonus = baseEffects.strengthBonus,
            defenceBonus = baseEffects.defenceBonus
        };
    }

    private static void ApplyNumericModifier(
        PlayerEffectModifier modifier,
        ref float additiveAccumulator,
        ref float multiplicativeAccumulator)
    {
        switch (modifier.modifierMode)
        {
            case PlayerEffectModifierMode.Additive:
                additiveAccumulator += modifier.numericValue;
                break;

            case PlayerEffectModifierMode.Multiplicative:
                multiplicativeAccumulator *= modifier.numericValue;
                break;
        }
    }

    private static void ApplyMultiplierOnlyModifier(
        PlayerEffectModifier modifier,
        ref float multiplicativeAccumulator)
    {
        switch (modifier.modifierMode)
        {
            case PlayerEffectModifierMode.Additive:
                multiplicativeAccumulator *= 1f + modifier.numericValue;
                break;

            case PlayerEffectModifierMode.Multiplicative:
                multiplicativeAccumulator *= modifier.numericValue;
                break;
        }
    }

    private static void ApplyBooleanModifier(
        PlayerEffectModifier modifier,
        ref bool boolAccumulator)
    {
        if (modifier.modifierMode == PlayerEffectModifierMode.OverrideTrue && modifier.boolValue)
        {
            boolAccumulator = true;
        }
    }
}