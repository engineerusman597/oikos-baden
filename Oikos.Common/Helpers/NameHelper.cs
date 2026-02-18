using Oikos.Common.Constants;

namespace Oikos.Common.Helpers;

public static class NameHelper
{
    public static string? NormalizeGender(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value!.Trim().ToLowerInvariant() switch
        {
            "female" or "f" or "w" or "mrs" or "ms" or "frau" => NameConstants.Genders.Female,
            "male" or "m" or "mr" or "herr" => NameConstants.Genders.Male,
            "diverse" or "d" or "divers" => NameConstants.Genders.Diverse,
            _ => null
        };
    }

    private static readonly string[][] TitleVariants = new[]
    {
        new[] { NameConstants.Titles.Mr, "Mr.", "Mr", "Herr" },
        new[] { NameConstants.Titles.MrsMs, "Ms./Mrs.", "Ms", "Mrs", "Frau" },
        new[] { NameConstants.Titles.Dr, "Dr.", "Dr" },
        new[] { NameConstants.Titles.Prof, "Prof.", "Prof" },
        new[] { NameConstants.Titles.ProfDr, "Prof. Dr.", "Prof Dr", "ProfDr" }
    };

    public static IReadOnlyList<string> TitleKeys { get; } = new[]
    {
        NameConstants.Titles.Mr,
        NameConstants.Titles.MrsMs,
        NameConstants.Titles.Dr,
        NameConstants.Titles.Prof,
        NameConstants.Titles.ProfDr
    };

    public static ParsedName ParseFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return new ParsedName(null, null, null);
        }

        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        string? titleKey = null;

        if (parts.Count > 0)
        {
            var firstToken = parts[0];
            foreach (var variantGroup in TitleVariants)
            {
                if (variantGroup.Skip(1).Any(v => string.Equals(v, firstToken, StringComparison.OrdinalIgnoreCase)))
                {
                    titleKey = variantGroup[0];
                    parts.RemoveAt(0);
                    break;
                }
            }
        }

        string? firstName = null;
        string? lastName = null;

        if (parts.Count > 0)
        {
            firstName = parts[0];
            if (parts.Count > 1)
            {
                lastName = string.Join(" ", parts.Skip(1));
            }
        }

        return new ParsedName(titleKey, firstName, lastName);
    }

    public static string ComposeFullName(string? titleKey, string? firstName, string? lastName, Func<string?, string?>? titleResolver)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(titleKey))
        {
            var display = titleResolver?.Invoke(titleKey) ?? titleKey;
            if (!string.IsNullOrWhiteSpace(display))
            {
                parts.Add(display.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            parts.Add(firstName!.Trim());
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            parts.Add(lastName!.Trim());
        }

        return string.Join(" ", parts);
    }

    public readonly record struct ParsedName(string? TitleKey, string? FirstName, string? LastName);
}
