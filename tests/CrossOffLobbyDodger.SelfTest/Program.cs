namespace CrossOff.LobbyDodger;

internal static class Program
{
    private static int Main()
    {
        var active = new BlacklistEntry
        {
            Id = "example-active",
            Group = "Example",
            Aliases = ["White <3 Bianca", "Südø"],
            Reason = "Self-test",
            EvidenceUrl = "https://example.invalid/evidence",
            AddedAt = DateTimeOffset.UtcNow,
            Active = true
        };

        var inactive = new BlacklistEntry
        {
            Id = "example-inactive",
            Group = "Example",
            Aliases = ["ShouldNotMatch"],
            Reason = "Self-test",
            EvidenceUrl = "https://example.invalid/evidence",
            AddedAt = DateTimeOffset.UtcNow,
            Active = false
        };

        AssertEqual("white3bianca", NameMatcher.Normalize("White <3 Bianca"), "normalization");
        AssertEqual("südø", NameMatcher.Normalize(" SÜDØ "), "Unicode normalization");
        AssertMatch("WHITE <3 BIANCA\r\nAnotherPlayer", [active], "White <3 Bianca", "case-insensitive line match");
        AssertMatch("prefix Südø suffix", [active], "Südø", "contained alias match");
        AssertNoMatch("ShouldNotMatch", [inactive], "inactive entry");
        AssertNoMatch("CompletelyDifferent", [active], "unrelated OCR text");

        Console.WriteLine("Lobby dodger matcher self-tests passed.");
        return 0;
    }

    private static void AssertMatch(
        string ocr,
        IEnumerable<BlacklistEntry> entries,
        string expectedAlias,
        string testName)
    {
        NameMatch? match = NameMatcher.FindMatch(ocr, entries);
        if (match?.Alias != expectedAlias)
        {
            throw new InvalidOperationException(
                $"Self-test failed ({testName}): expected '{expectedAlias}', got '{match?.Alias ?? "no match"}'.");
        }
    }

    private static void AssertNoMatch(string ocr, IEnumerable<BlacklistEntry> entries, string testName)
    {
        NameMatch? match = NameMatcher.FindMatch(ocr, entries);
        if (match is not null)
        {
            throw new InvalidOperationException(
                $"Self-test failed ({testName}): unexpectedly matched '{match.Alias}'.");
        }
    }

    private static void AssertEqual(string expected, string actual, string testName)
    {
        if (!expected.Equals(actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Self-test failed ({testName}): expected '{expected}', got '{actual}'.");
        }
    }
}
