using UnityEngine;

public readonly struct PlayerAbilitySlotSnapshot
{
    public readonly int SlotIndex;
    public readonly PlayerAbilitySO Definition;
    public readonly Sprite Icon;
    public readonly string AbilityName;
    public readonly bool HasAbility;
    public readonly bool IsUnlocked;
    public readonly bool IsOnCooldown;
    public readonly bool CanAfford;
    public readonly float CooldownDuration;
    public readonly float CooldownRemaining;
    public readonly float ResourceCost;
    public readonly float CurrentResource;
    public readonly float MaxResource;

    public float CooldownRemainingNormalized =>
        CooldownDuration > 0f ? Mathf.Clamp01(CooldownRemaining / CooldownDuration) : 0f;

    public PlayerAbilitySlotSnapshot(
        int slotIndex,
        PlayerAbilitySO definition,
        bool isUnlocked,
        bool isOnCooldown,
        bool canAfford,
        float cooldownDuration,
        float cooldownRemaining,
        float resourceCost,
        float currentResource,
        float maxResource)
    {
        SlotIndex = slotIndex;
        Definition = definition;
        Icon = definition != null ? definition.icon : null;
        AbilityName = definition != null ? definition.abilityName : string.Empty;
        HasAbility = definition != null;
        IsUnlocked = isUnlocked;
        IsOnCooldown = isOnCooldown;
        CanAfford = canAfford;
        CooldownDuration = Mathf.Max(0f, cooldownDuration);
        CooldownRemaining = Mathf.Max(0f, cooldownRemaining);
        ResourceCost = Mathf.Max(0f, resourceCost);
        CurrentResource = Mathf.Max(0f, currentResource);
        MaxResource = Mathf.Max(0f, maxResource);
    }

    public static PlayerAbilitySlotSnapshot Empty(int slotIndex)
    {
        return new PlayerAbilitySlotSnapshot(
            slotIndex,
            null,
            false,
            false,
            false,
            0f,
            0f,
            0f,
            0f,
            0f);
    }
}
