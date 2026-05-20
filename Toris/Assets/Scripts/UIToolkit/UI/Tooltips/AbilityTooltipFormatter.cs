using System.Globalization;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    public static class AbilityTooltipFormatter
    {
        public static ItemTooltipData Build(PlayerAbilitySlotSnapshot snapshot)
        {
            if (!snapshot.HasAbility || snapshot.Definition == null)
                return null;

            string title = string.IsNullOrWhiteSpace(snapshot.AbilityName)
                ? "Ability"
                : snapshot.AbilityName;

            ItemTooltipData data = new ItemTooltipData(title, GetDescription(snapshot.Definition));

            data.AddRow("Slot", (snapshot.SlotIndex + 1).ToString(CultureInfo.InvariantCulture));
            data.AddRow("Status", GetStatus(snapshot));

            if (snapshot.ResourceCost > 0f)
                data.AddRow("Stamina Cost", FormatNumber(snapshot.ResourceCost));

            if (snapshot.CooldownDuration > 0f)
                data.AddRow("Cooldown", $"{FormatNumber(snapshot.CooldownDuration)}s");

            if (snapshot.IsOnCooldown)
                data.AddRow("Remaining", $"{FormatNumber(snapshot.CooldownRemaining)}s");

            if (snapshot.Definition.requiredSkill != null)
                data.AddRow("Required Skill", snapshot.Definition.requiredSkill.skillID);

            if (snapshot.Definition.blocksBowDraw)
                data.AddRow("Bow Draw Lock", $"{FormatNumber(snapshot.Definition.bowDrawLockDuration)}s");

            if (snapshot.Definition.blocksMovement)
                data.AddRow("Movement Lock", $"{FormatNumber(snapshot.Definition.movementLockDuration)}s");

            return data;
        }

        private static string GetDescription(PlayerAbilitySO ability)
        {
            if (ability.requiredSkill != null && !string.IsNullOrWhiteSpace(ability.requiredSkill.description))
                return ability.requiredSkill.description;

            return "No description.";
        }

        private static string GetStatus(PlayerAbilitySlotSnapshot snapshot)
        {
            if (!snapshot.IsUnlocked)
                return "Locked";

            if (!snapshot.CanAfford)
                return "Low Stamina";

            if (snapshot.IsOnCooldown)
                return "Cooling Down";

            return "Ready";
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
