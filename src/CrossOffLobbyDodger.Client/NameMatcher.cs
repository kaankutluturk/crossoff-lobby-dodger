using System.Globalization;
using System.Text;

namespace CrossOff.LobbyDodger;

public static class NameMatcher
{
    public static NameMatch? FindMatch(string ocrText, IEnumerable<BlacklistEntry> entries)
    {
        string[] lines = ocrText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (BlacklistEntry entry in entries.Where(static entry => entry.Active))
        {
            foreach (string alias in entry.Aliases)
            {
                string normalizedAlias = Normalize(alias);
                if (normalizedAlias.Length < 3)
                {
                    continue;
                }

                foreach (string line in lines)
                {
                    string normalizedLine = Normalize(line);
                    if (normalizedLine.Equals(normalizedAlias, StringComparison.Ordinal) ||
                        (normalizedAlias.Length >= 4 && normalizedLine.Contains(normalizedAlias, StringComparison.Ordinal)))
                    {
                        return new NameMatch(entry, alias, line);
                    }
                }
            }
        }

        return null;
    }

    public static string Normalize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string compatibilityNormalized = value.Normalize(NormalizationForm.FormKC);
        var result = new StringBuilder(compatibilityNormalized.Length);

        foreach (Rune rune in compatibilityNormalized.EnumerateRunes())
        {
            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.UppercaseLetter or
                UnicodeCategory.LowercaseLetter or
                UnicodeCategory.TitlecaseLetter or
                UnicodeCategory.ModifierLetter or
                UnicodeCategory.OtherLetter or
                UnicodeCategory.DecimalDigitNumber)
            {
                result.Append(rune.ToString().ToLowerInvariant());
            }
        }

        return result.ToString();
    }
}
