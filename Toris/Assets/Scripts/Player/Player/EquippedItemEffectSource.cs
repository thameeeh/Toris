using System.Collections.Generic;
using OutlandHaven.Inventory;

public sealed class EquippedItemEffectSource : IPlayerEffectSource
{
    private readonly string _sourceKey;
    private readonly ItemInstance _item;

    public string SourceKey => _sourceKey;

    public EquippedItemEffectSource(string sourceKey, ItemInstance item)
    {
        _sourceKey = sourceKey;
        _item = item;
    }

    public void CollectModifiers(List<PlayerEffectModifier> modifiers)
    {
        if (modifiers == null || _item == null || _item.BaseItem == null)
            return;

        EquippedItemComputedStats stats = EquippedItemStatCalculator.Calculate(_item);

        if (!stats.IsValid)
            return;

        if (stats.StrengthBonus != 0f)
        {
            modifiers.Add(new PlayerEffectModifier(
                PlayerEffectType.OutgoingDamageMultiplier,
                PlayerEffectModifierMode.Additive,
                stats.StrengthBonus * 0.01f));
        }

        float defensiveValue = stats.DefenceBonus + stats.PhysicalDefense;

        if (defensiveValue != 0f)
        {
            modifiers.Add(new PlayerEffectModifier(
                PlayerEffectType.IncomingDamageMultiplier,
                PlayerEffectModifierMode.Additive,
                -defensiveValue * 0.01f));
        }
    }
}