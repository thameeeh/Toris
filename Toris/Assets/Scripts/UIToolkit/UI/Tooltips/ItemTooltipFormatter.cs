using System.Globalization;
using System.Text;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    public static class ItemTooltipFormatter
    {
        public static ItemTooltipData Build(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty || slot.HeldItem?.BaseItem == null)
                return null;

            InventoryItemSO item = slot.HeldItem.BaseItem;
            string title = FormatItemName(item.ItemName, item.name);
            string description = string.IsNullOrWhiteSpace(item.Description) ? "No description." : item.Description;
            ItemTooltipData data = new ItemTooltipData(title, description);

            AddBasicRows(data, item, slot);
            AddConsumableRows(data, item);
            AddEquipmentRows(data, item);
            AddProgressionRows(data, item);
            AddStateRows(data, slot);

            return data;
        }

        private static void AddBasicRows(ItemTooltipData data, InventoryItemSO item, InventorySlot slot)
        {
            if (slot.Count > 1)
                data.AddRow("Count", slot.Count.ToString(CultureInfo.InvariantCulture));

            data.AddRow("Value", $"{item.GoldValue.ToString(CultureInfo.InvariantCulture)}g");

            if (item.MaxStackSize > 1)
                data.AddRow("Max Stack", item.MaxStackSize.ToString(CultureInfo.InvariantCulture));
        }

        private static void AddConsumableRows(ItemTooltipData data, InventoryItemSO item)
        {
            ConsumableComponent consumable = item.GetComponent<ConsumableComponent>();
            if (consumable == null)
                return;

            if (consumable.EffectMode == ConsumableEffectMode.InstantResource && consumable.amount > 0)
            {
                data.AddRow("Restores", $"{consumable.amount.ToString(CultureInfo.InvariantCulture)} {FormatConsumptionSlot(consumable.EffectPayload)}");
            }
            else if (consumable.EffectMode == ConsumableEffectMode.TimedPlayerEffect)
            {
                AddTimedEffectRows(data, consumable);
            }

            if (consumable.TimedEffectDuration > 0f && consumable.EffectMode == ConsumableEffectMode.TimedPlayerEffect)
                data.AddRow("Duration", FormatSeconds(consumable.TimedEffectDuration));

            if (consumable.CooldownDuration > 0f)
                data.AddRow("Cooldown", FormatSeconds(consumable.CooldownDuration));
        }

        private static void AddTimedEffectRows(ItemTooltipData data, ConsumableComponent consumable)
        {
            PlayerEffectDefinitionSO effectDefinition = consumable.ResolveTimedEffectDefinition();
            if (effectDefinition == null || effectDefinition.Modifiers == null || effectDefinition.Modifiers.Count == 0)
            {
                data.AddRow("Effect", "Timed Buff");
                return;
            }

            for (int i = 0; i < effectDefinition.Modifiers.Count; i++)
            {
                PlayerEffectModifier modifier = effectDefinition.Modifiers[i];
                data.AddRow(HumanizeIdentifier(modifier.effectType.ToString()), FormatModifierValue(modifier));
            }
        }

        private static void AddEquipmentRows(ItemTooltipData data, InventoryItemSO item)
        {
            EquipableComponent equipable = item.GetComponent<EquipableComponent>();
            if (equipable != null)
            {
                data.AddRow("Slot", HumanizeIdentifier(equipable.TargetSlot.ToString()));
                AddSignedRow(data, "Strength", equipable.StrengthBonus);
                AddSignedRow(data, "Defense", equipable.DefenceBonus);
                AddSignedRow(data, "Max Health", equipable.MaxHealthBonus);
                AddSignedRow(data, "Max Stamina", equipable.MaxStaminaBonus);
            }

            OffensiveComponent offensive = item.GetComponent<OffensiveComponent>();
            if (offensive != null)
            {
                data.AddRow("Damage", FormatNumber(offensive.BaseDamage));
                data.AddRow("Attack Speed", $"{FormatNumber(offensive.AttackSpeed)}/s");
            }

            DefensiveComponent defensive = item.GetComponent<DefensiveComponent>();
            if (defensive != null)
            {
                AddSignedRow(data, "Physical Defense", defensive.PhysicalDefense);
                AddSignedRow(data, "Magic Defense", defensive.MagicalDefense);
            }
        }

        private static void AddProgressionRows(ItemTooltipData data, InventoryItemSO item)
        {
            ProgressionComponent progression = item.GetComponent<ProgressionComponent>();
            if (progression != null)
                data.AddRow("Category", HumanizeIdentifier(progression.Category.ToString()));
        }

        private static void AddStateRows(ItemTooltipData data, InventorySlot slot)
        {
            InventoryItemSO item = slot.HeldItem.BaseItem;

            UpgradeableComponent upgradeable = item.GetComponent<UpgradeableComponent>();
            UpgradeableState upgradeState = slot.HeldItem.GetState<UpgradeableState>();
            if (upgradeable != null)
            {
                int currentLevel = upgradeState != null ? upgradeState.CurrentLevel : 1;
                data.AddRow("Level", $"{currentLevel.ToString(CultureInfo.InvariantCulture)} / {upgradeable.MaxLevel.ToString(CultureInfo.InvariantCulture)}");
            }

            EvolvingComponent evolving = item.GetComponent<EvolvingComponent>();
            EvolvingState evolvingState = slot.HeldItem.GetState<EvolvingState>();
            if (evolving != null)
            {
                int currentKills = evolvingState != null ? evolvingState.CurrentKills : 0;
                data.AddRow("Awakening", $"{currentKills.ToString(CultureInfo.InvariantCulture)} / {evolving.KillsRequired.ToString(CultureInfo.InvariantCulture)}");
                if (evolvingState != null && evolvingState.IsAwakened)
                    data.AddRow("Awakened", "Yes");
                AddSignedRow(data, "Awakened Damage", evolving.AwakenedDamageBonus);
            }
        }

        private static void AddSignedRow(ItemTooltipData data, string label, float value)
        {
            if (value == 0f)
                return;

            string sign = value > 0f ? "+" : "";
            data.AddRow(label, $"{sign}{FormatNumber(value)}");
        }

        private static string FormatConsumptionSlot(ConsumptionSlot slot)
        {
            return slot == ConsumptionSlot.Mana ? "Stamina" : "Health";
        }

        private static string FormatModifierValue(PlayerEffectModifier modifier)
        {
            switch (modifier.modifierMode)
            {
                case PlayerEffectModifierMode.Additive:
                    return FormatAdditiveModifier(modifier);
                case PlayerEffectModifierMode.Multiplicative:
                    return $"{FormatNumber(modifier.numericValue)}x";
                case PlayerEffectModifierMode.OverrideTrue:
                    return modifier.boolValue ? "Yes" : "No";
                default:
                    return FormatNumber(modifier.numericValue);
            }
        }

        private static string FormatAdditiveModifier(PlayerEffectModifier modifier)
        {
            string sign = modifier.numericValue >= 0f ? "+" : "";
            string suffix = modifier.effectType == PlayerEffectType.HealthRegenPerSecond ||
                            modifier.effectType == PlayerEffectType.StaminaRegenPerSecond
                ? "/s"
                : "";

            return $"{sign}{FormatNumber(modifier.numericValue)}{suffix}";
        }

        private static string FormatSeconds(float seconds)
        {
            return $"{FormatNumber(seconds)}s";
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatItemName(string itemName, string fallback)
        {
            string source = string.IsNullOrWhiteSpace(itemName) ? fallback : itemName;
            if (string.IsNullOrWhiteSpace(source))
                return "Unknown Item";

            string spaced = source.Replace('_', ' ').Replace('-', ' ').Trim().ToLowerInvariant();
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced);
        }

        private static string HumanizeIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return string.Empty;

            string normalized = identifier.Replace('_', ' ').Replace('-', ' ');
            StringBuilder builder = new StringBuilder(normalized.Length + 8);

            for (int i = 0; i < normalized.Length; i++)
            {
                char current = normalized[i];
                if (i > 0 && char.IsUpper(current))
                {
                    char previous = normalized[i - 1];
                    bool nextIsLower = i + 1 < normalized.Length && char.IsLower(normalized[i + 1]);
                    if (char.IsLower(previous) || char.IsDigit(previous) || nextIsLower)
                        builder.Append(' ');
                }

                builder.Append(current);
            }

            return builder.ToString();
        }
    }
}
