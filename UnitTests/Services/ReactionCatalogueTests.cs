using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Predictathon.Application.Services;

namespace Predictathon.UnitTests.Services;

/// <summary>
/// Tests <see cref="ReactionCatalogue"/> against the real, checked-in image set rather than a
/// fixture: the whole point of the class is that emoji-mart's codepoint spellings and Twemoji's
/// filenames disagree in undocumented ways, which only the actual files on disk can settle.
/// </summary>
public class ReactionCatalogueTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    private static ReactionCatalogue MakeCatalogue()
    {
        var assetsPath = Path.Combine(RepositoryRoot, "WebApi", "Assets", "Reactions");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Reactions:AssetsPath"] = assetsPath })
            .Build();

        return new ReactionCatalogue(configuration);
    }

    /// <summary>
    /// Walks up from this source file to the folder holding the solution file. Anchored on
    /// <see cref="CallerFilePathAttribute"/> rather than AppContext.BaseDirectory so the assets
    /// are still found when the build output lives outside the repository (a redirected
    /// BaseOutputPath, for instance).
    /// </summary>
    /// <param name="sourceFilePath">Compile-time path of this file; never passed explicitly.</param>
    private static string FindRepositoryRoot([CallerFilePath] string sourceFilePath = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(sourceFilePath)!);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Predictathon.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException($"Could not locate the repository root (no Predictathon.slnx found above '{sourceFilePath}').");
        }

        return directory.FullName;
    }

    /// <summary>
    /// The regression guard for the bug this whole scheme exists to prevent: every emoji the
    /// picker can offer must resolve to a file that actually exists. The identity list is a
    /// snapshot of @emoji-mart/data's sets/15 dataset - regenerate it if that package is upgraded
    /// (see UnitTests/TestData/emoji-mart-unified-15.txt).
    /// </summary>
    [Fact]
    public void ResolveImageFile_EveryEmojiInTheShippedDataset_ResolvesToAFileOnDisk()
    {
        var catalogue = MakeCatalogue();
        var identities = File.ReadAllLines(Path.Combine(RepositoryRoot, "UnitTests", "TestData", "emoji-mart-unified-15.txt"))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        identities.Should().HaveCountGreaterThan(3000, "the dataset snapshot should cover the whole emoji set");

        var unresolved = identities.Where(u => catalogue.ResolveImageFile($"u:{u}") is null).ToList();

        unresolved.Should().BeEmpty(
            "every emoji the picker offers must map onto a vendored Twemoji file - an unresolved one renders as a dead image");
    }

    [Theory]
    // The original bug: emoji-mart says "2764-fe0f", Twemoji ships "2764.svg".
    [InlineData("u:2764-fe0f", "2764.svg")]
    // Unaffected - no variation selector to reconcile in the first place.
    [InlineData("u:1f44d", "1f44d.svg")]
    // Keeps FE0F, because this ZWJ sequence's file does too.
    [InlineData("u:2764-fe0f-200d-1f525", "2764-fe0f-200d-1f525.svg")]
    // Drops FE0F despite being a ZWJ sequence - the exception that rules out a tidy rule.
    [InlineData("u:1f441-fe0f-200d-1f5e8-fe0f", "1f441-200d-1f5e8.svg")]
    // Twemoji writes codepoints unpadded, emoji-mart pads them.
    [InlineData("u:00a9-fe0f", "a9.svg")]
    [InlineData("u:0031-fe0f-20e3", "31-20e3.svg")]
    public void ResolveImageFile_StandardEmoji_MapsOntoTheTwemojiFilename(string reactionId, string expectedFile)
    {
        MakeCatalogue().ResolveImageFile(reactionId).Should().Be(expectedFile);
    }

    [Theory]
    [InlineData("u:1f3f4-e0067-e0062-e0065-e006e-e0067-e007f", "1f3f4-e0067-e0062-e0065-e006e-e0067-e007f.svg")]
    [InlineData("u:1f3f4-e0067-e0062-e0073-e0063-e0074-e007f", "1f3f4-e0067-e0062-e0073-e0063-e0074-e007f.svg")]
    [InlineData("u:1f3f4-e0067-e0062-e0077-e006c-e0073-e007f", "1f3f4-e0067-e0062-e0077-e006c-e0073-e007f.svg")]
    public void ResolveImageFile_UkSubdivisionFlags_ResolveAsStandardEmoji(string reactionId, string expectedFile)
    {
        MakeCatalogue().ResolveImageFile(reactionId).Should().Be(expectedFile);
    }

    [Fact]
    public void GetCustomReactions_DoesNotDuplicateTheUkSubdivisionFlags()
    {
        // They're pickable as standard emoji, so listing them as custom entries too made one flag
        // reachable under two identities - and therefore rendered as two separate pills.
        var customIds = MakeCatalogue().GetCustomReactions().Select(c => c.Id).ToList();

        customIds.Should().NotContain(id => id.Contains("flag", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetCustomReactions_ReturnsTheManifestAndEveryEntryResolves()
    {
        var catalogue = MakeCatalogue();

        var customReactions = catalogue.GetCustomReactions();

        customReactions.Should().NotBeEmpty();
        customReactions.Should().Contain(c => c.Id == "ludo" && c.ImageFile == "ludo.png");
        customReactions.Should().OnlyContain(c => catalogue.ResolveImageFile($"c:{c.Id}") == c.ImageFile);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("2764-fe0f")]                 // Unnamespaced - not a valid identity.
    [InlineData("c:not-a-real-reaction")]
    [InlineData("u:not-hex")]
    [InlineData("u:../../appsettings.json")]  // Must never escape the assets folder.
    [InlineData("u:2764-fe0f/../../secret")]
    [InlineData("x:2764")]
    public void ResolveImageFile_UnknownOrHostileIdentity_ReturnsNull(string reactionId)
    {
        MakeCatalogue().ResolveImageFile(reactionId).Should().BeNull();
    }

    [Theory]
    // The split that was live in production: the legacy site's spelling and the picker's spelling
    // of the red heart both reduce to the one identity.
    [InlineData("u:2764-fe0f", "u:2764")]
    [InlineData("u:2764", "u:2764")]
    // Padded/unpadded, and the keycaps.
    [InlineData("u:00a9-fe0f", "u:a9")]
    [InlineData("u:a9", "u:a9")]
    [InlineData("u:0031-fe0f-20e3", "u:31-20e3")]
    // ZWJ sequences that legitimately keep FE0F are already canonical.
    [InlineData("u:2764-fe0f-200d-1f525", "u:2764-fe0f-200d-1f525")]
    // ...and the one that drops it despite being a ZWJ sequence.
    [InlineData("u:1f441-fe0f-200d-1f5e8-fe0f", "u:1f441-200d-1f5e8")]
    // Custom reactions have only one spelling.
    [InlineData("c:ludo", "c:ludo")]
    public void Canonicalise_ReducesEverySpellingOfAnEmojiToOneIdentity(string reactionId, string expected)
    {
        MakeCatalogue().Canonicalise(reactionId).Should().Be(expected);
    }

    [Theory]
    [InlineData("u:not-hex")]
    [InlineData("c:not-a-real-reaction")]
    [InlineData("u:../../appsettings.json")]
    [InlineData("")]
    public void Canonicalise_UnknownIdentity_ReturnsNull(string reactionId)
    {
        MakeCatalogue().Canonicalise(reactionId).Should().BeNull();
    }

    /// <summary>
    /// Canonicalising must be a fixed point - feeding a canonical identity back in has to return
    /// it unchanged, or the migration and the add path would keep rewriting rows forever.
    /// </summary>
    [Fact]
    public void Canonicalise_IsIdempotentAcrossTheWholeDataset()
    {
        var catalogue = MakeCatalogue();
        var identities = ReadDatasetIdentities();

        var unstable = identities
            .Select(u => catalogue.Canonicalise($"u:{u}"))
            .OfType<string>()
            .Where(canonical => catalogue.Canonicalise(canonical) != canonical)
            .ToList();

        unstable.Should().BeEmpty("canonicalising an already-canonical identity must be a no-op");
    }

    /// <summary>
    /// The SQL migration can't compute canonical forms itself (the Twemoji filename rule has
    /// exceptions), so it carries a generated mapping. This asserts that mapping still agrees with
    /// ReactionCatalogue - if the assets are re-vendored and the two drift apart, production data
    /// would be rewritten to identities the app no longer resolves.
    /// </summary>
    [Fact]
    public void CanonicalisationMigration_MappingAgreesWithTheCatalogue()
    {
        var catalogue = MakeCatalogue();
        var sql = File.ReadAllText(Path.Combine(
            RepositoryRoot, "Database", "Post-Deployment", "Migrations", "02_CanonicaliseMessageReactionIdentity.sql"));

        var pairs = Regex.Matches(sql, @"^\s*\('(?<from>[^']+)',\s*'(?<to>[^']+)'\)", RegexOptions.Multiline)
            .Select(m => (From: m.Groups["from"].Value, To: m.Groups["to"].Value))
            .ToList();

        pairs.Should().NotBeEmpty("the migration should carry a generated mapping");

        var disagreements = pairs
            .Where(p => catalogue.Canonicalise(p.From) != p.To)
            .Select(p => $"{p.From} -> {p.To} (catalogue says {catalogue.Canonicalise(p.From) ?? "null"})")
            .ToList();

        disagreements.Should().BeEmpty();
    }

    /// <summary>
    /// Every non-canonical spelling the picker can produce must be in the migration's mapping,
    /// otherwise those rows stay split after the migration runs.
    /// </summary>
    [Fact]
    public void CanonicalisationMigration_CoversEveryNonCanonicalSpellingThePickerCanProduce()
    {
        var catalogue = MakeCatalogue();
        var sql = File.ReadAllText(Path.Combine(
            RepositoryRoot, "Database", "Post-Deployment", "Migrations", "02_CanonicaliseMessageReactionIdentity.sql"));

        var mapped = Regex.Matches(sql, @"^\s*\('(?<from>[^']+)',\s*'(?<to>[^']+)'\)", RegexOptions.Multiline)
            .Select(m => m.Groups["from"].Value)
            .ToHashSet(StringComparer.Ordinal);

        var missing = ReadDatasetIdentities()
            .Select(u => $"u:{u}")
            .Where(id => catalogue.Canonicalise(id) is string canonical && canonical != id)
            .Where(id => !mapped.Contains(id))
            .ToList();

        missing.Should().BeEmpty("every spelling that needs rewriting must appear in the migration's mapping");
    }

    private static List<string> ReadDatasetIdentities()
        => File.ReadAllLines(Path.Combine(RepositoryRoot, "UnitTests", "TestData", "emoji-mart-unified-15.txt"))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
}
