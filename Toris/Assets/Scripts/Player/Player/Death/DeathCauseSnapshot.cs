using System;
using System.Globalization;
using System.Text;
using UnityEngine;

// Death screen related: lightweight runtime payload describing what last damaged the player.
public readonly struct DeathCauseSnapshot
{
    public const string UnknownDisplayName = "Unknown";

    public DeathCauseSnapshot(string displayName, DeathCauseKind kind)
    {
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? UnknownDisplayName
            : displayName.Trim();
        Kind = kind;
    }

    public string DisplayName { get; }
    public DeathCauseKind Kind { get; }
    public bool HasKnownCause => !string.IsNullOrWhiteSpace(DisplayName)
        && !string.Equals(DisplayName, UnknownDisplayName, StringComparison.Ordinal);

    public static DeathCauseSnapshot Unknown()
    {
        return new DeathCauseSnapshot(UnknownDisplayName, DeathCauseKind.Unknown);
    }

    public static DeathCauseSnapshot FromHit(in HitData hit)
    {
        return FromGameObject(hit.source);
    }

    public static DeathCauseSnapshot FromGameObject(GameObject source)
    {
        if (source == null)
            return Unknown();

        Enemy enemy = source.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            string enemyTypeName = enemy.GetType().Name;
            return new DeathCauseSnapshot(HumanizeIdentifier(enemyTypeName), DeathCauseKind.DirectHit);
        }

        return new DeathCauseSnapshot(HumanizeIdentifier(source.name), DeathCauseKind.DirectHit);
    }

    public static DeathCauseSnapshot FromStatus(PlayerStatusEffectType statusType)
    {
        return new DeathCauseSnapshot(HumanizeIdentifier(statusType.ToString()), DeathCauseKind.StatusEffect);
    }

    private static string HumanizeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return UnknownDisplayName;

        string normalized = identifier
            .Replace("(Clone)", string.Empty)
            .Replace('_', ' ')
            .Replace('-', ' ')
            .Trim();

        if (string.IsNullOrWhiteSpace(normalized))
            return UnknownDisplayName;

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

        string spaced = builder.ToString().Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(spaced)
            ? UnknownDisplayName
            : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced);
    }
}

public enum DeathCauseKind
{
    Unknown,
    DirectHit,
    StatusEffect
}
