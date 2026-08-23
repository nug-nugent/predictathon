using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Predictathon.Application.Attributes;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;

namespace Predictathon.Application.Services;

/// <summary>
/// Resolves reaction identities against the vendored image set on disk. See
/// <see cref="IReactionCatalogue"/> for why the server, not the client, owns this.
/// </summary>
[ScopedService]
public partial class ReactionCatalogue : IReactionCatalogue
{
    private const string CustomManifestFileName = "custom-reactions.json";

    private static readonly JsonSerializerOptions ManifestJsonOptions = new() { PropertyNameCaseInsensitive = true };

    // The asset folder is static, checked-in content that never changes while the app is running,
    // so the directory listing (~4,000 files) and manifest are read once per path and cached for
    // the process lifetime rather than on every scoped resolve.
    private static readonly ConcurrentDictionary<string, CatalogueData> Cache = new();

    private readonly IConfiguration _configuration;

    /// <summary>
    /// Creates the catalogue.
    /// </summary>
    /// <param name="configuration">Application configuration, for the reaction assets path.</param>
    public ReactionCatalogue(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public IReadOnlyList<CustomReactionModel> GetCustomReactions() => GetData().CustomReactions;

    /// <inheritdoc />
    public string? Canonicalise(string reactionId)
    {
        var imageFile = ResolveImageFile(reactionId);
        if (imageFile is null)
        {
            return null;
        }

        // A custom reaction's id is already the one spelling it has.
        if (reactionId.StartsWith("c:", StringComparison.Ordinal))
        {
            return reactionId;
        }

        return $"u:{Path.GetFileNameWithoutExtension(imageFile)}";
    }

    /// <inheritdoc />
    public string? ResolveImageFile(string reactionId)
    {
        if (string.IsNullOrWhiteSpace(reactionId))
        {
            return null;
        }

        var data = GetData();

        if (reactionId.StartsWith("c:", StringComparison.Ordinal))
        {
            var customId = reactionId[2..];
            return data.CustomReactionsById.TryGetValue(customId, out var custom) && data.Files.Contains(custom.ImageFile)
                ? custom.ImageFile
                : null;
        }

        if (reactionId.StartsWith("u:", StringComparison.Ordinal))
        {
            return ResolveUnicodeImageFile(reactionId[2..], data.Files);
        }

        return null;
    }

    /// <summary>
    /// Maps an emoji-mart <c>unified</c> codepoint sequence onto the filename Twemoji actually
    /// ships it under.
    ///
    /// The two naming schemes disagree in ways that are not documented anywhere and were derived
    /// by diffing the dataset against the vendored files:
    ///  - Twemoji writes codepoints as unpadded hex (<c>a9</c>), emoji-mart pads them (<c>00a9</c>).
    ///  - Twemoji usually drops the FE0F variation selector (<c>2764.svg</c> for the red heart),
    ///    but keeps it on most ZWJ sequences (<c>2764-fe0f-200d-1f525.svg</c>) - and not even
    ///    consistently there (<c>1f441-200d-1f5e8.svg</c> keeps neither).
    ///
    /// Rather than trying to reimplement that as a rule, this tries the plausible spellings in
    /// order and takes whichever one exists on disk. Verified in ReactionCatalogueTests to resolve
    /// every skin in the shipped emoji-mart dataset.
    /// </summary>
    /// <param name="unified">The hyphen-separated codepoint sequence, e.g. <c>2764-fe0f</c>.</param>
    /// <param name="files">The set of filenames present in the assets folder.</param>
    private static string? ResolveUnicodeImageFile(string unified, IReadOnlySet<string> files)
    {
        // Rejects anything that isn't a codepoint sequence outright, so a hostile identity can
        // never walk out of the assets folder via the filename we build from it.
        if (!UnifiedPattern().IsMatch(unified))
        {
            return null;
        }

        var parts = unified.Split('-');
        var unpadded = parts.Select(p => p.TrimStart('0')).ToArray();

        string[] candidates =
        [
            string.Join('-', parts),
            string.Join('-', unpadded),
            string.Join('-', unpadded.Where(p => p != "fe0f")),
        ];

        foreach (var candidate in candidates)
        {
            var fileName = $"{candidate}.svg";
            if (files.Contains(fileName))
            {
                return fileName;
            }
        }

        return null;
    }

    private CatalogueData GetData()
    {
        var assetsPath = Path.GetFullPath(_configuration["Reactions:AssetsPath"] ?? Path.Combine("Assets", "Reactions"));
        return Cache.GetOrAdd(assetsPath, Load);
    }

    private static CatalogueData Load(string assetsPath)
    {
        var files = Directory.Exists(assetsPath)
            ? Directory.EnumerateFiles(assetsPath).Select(Path.GetFileName).OfType<string>().ToHashSet(StringComparer.Ordinal)
            : [];

        var manifestPath = Path.Combine(assetsPath, CustomManifestFileName);
        List<CustomReactionModel> customReactions = [];
        if (File.Exists(manifestPath))
        {
            customReactions = JsonSerializer.Deserialize<List<CustomReactionModel>>(File.ReadAllText(manifestPath), ManifestJsonOptions) ?? [];
        }

        // Only surface custom reactions whose image is actually present, so a manifest typo shows
        // up as a missing picker entry rather than a dead image on the board.
        customReactions = customReactions.Where(c => files.Contains(c.ImageFile)).ToList();

        return new CatalogueData(
            files,
            customReactions,
            customReactions.ToDictionary(c => c.Id, StringComparer.Ordinal));
    }

    [GeneratedRegex("^[0-9a-f]+(-[0-9a-f]+)*$")]
    private static partial Regex UnifiedPattern();

    private sealed record CatalogueData(
        IReadOnlySet<string> Files,
        IReadOnlyList<CustomReactionModel> CustomReactions,
        IReadOnlyDictionary<string, CustomReactionModel> CustomReactionsById);
}
