// Death screen related: converts the raw death cause into display copy for the overlay.
public static class DeathCauseMessageFormatter
{
    public const string DefaultSubtitle = "The run ends here.";

    public static string FormatSubtitle(DeathCauseSnapshot cause)
    {
        if (!cause.HasKnownCause)
            return DefaultSubtitle;

        string key = NormalizeCause(cause.DisplayName);

        if (key.Contains("wolf"))
            return "You were crunchy.";

        if (key.Contains("boar"))
            return "The tusks were not decorative.";

        if (key.Contains("necromancer"))
            return "You got turned into the next blood mage.";

        if (key.Contains("blood mage"))
            return "The blood mage found your health bar very cooperative.";

        if (key.Contains("deer"))
            return "Somehow, the deer killed you?!";

        if (key.Contains("poison"))
            return "Poison damage is just a loading bar for death.";

        if (key.Contains("burn"))
            return "Fire tested your build. Fire won.";

        if (key.Contains("bleed"))
            return "Your blood left early to avoid the penalty.";

        return cause.Kind == DeathCauseKind.StatusEffect
            ? $"{cause.DisplayName} remembered it was damage over time."
            : $"{cause.DisplayName} has been added to your list of concerns.";
    }

    private static string NormalizeCause(string cause)
    {
        return string.IsNullOrWhiteSpace(cause)
            ? string.Empty
            : cause.Trim().ToLowerInvariant();
    }
}
