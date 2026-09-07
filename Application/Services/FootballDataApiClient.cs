using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Predictathon.Application.Attributes;
using Predictathon.Application.Common;
using Predictathon.Application.Exceptions;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Models;
using Predictathon.Application.Options;

namespace Predictathon.Application.Services;

/// <summary>
/// <see cref="IExternalMatchDataService"/> implementation backed by the football-data.org v4 API
/// (free tier). Only this class should know about football-data.org's specific request/response
/// shape - callers depend on the provider-agnostic interface.
/// </summary>
[ScopedService]
public class FootballDataApiClient : IExternalMatchDataService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<FootballDataApiOptions> _options;
    private readonly IExternalApiRateLimiter _rateLimiter;

    public FootballDataApiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<FootballDataApiOptions> options,
        IExternalApiRateLimiter rateLimiter)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _rateLimiter = rateLimiter;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExternalFixture>> GetFixturesAsync(string competitionCode, int season, CancellationToken cancellationToken = default)
    {
        var payload = await GetAsync<MatchesResponse>(
            $"competitions/{competitionCode}/matches?season={season}", cancellationToken);

        return (payload?.Matches ?? [])
            .Select(m => new ExternalFixture
            {
                ExternalMatchID = m.Id,
                KickoffUtc = m.UtcDate,
                IsKickoffConfirmed = m.Status != ScheduledStatus,
                HomeTeamExternalCode = m.HomeTeam.Id.ToString(),
                AwayTeamExternalCode = m.AwayTeam.Id.ToString(),
                HomeTeamName = m.HomeTeam.Name,
                AwayTeamName = m.AwayTeam.Name,
                GroupName = ToGroupName(m.Group),
                IsKnockout = KnockoutStageNames.ContainsKey(m.Stage ?? ""),
                Description = ToStageDescription(m.Stage, m.Group),
                KnockoutRound = ToKnockoutRound(m.Stage),
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExternalMatchScore>> GetScoresAsync(
        string competitionCode,
        DateOnly fromUtcDate,
        DateOnly toUtcDate,
        CancellationToken cancellationToken = default)
    {
        // One competition's fixtures in a single request, rather than one request per match: the
        // whole point of the rate limit is that calls are scarce, and every live match in a
        // competition arrives in the same response. Same endpoint family as GetFixturesAsync, which
        // matters because the free tier grants access per endpoint.
        var payload = await GetAsync<MatchesResponse>(
            $"competitions/{competitionCode}/matches?dateFrom={fromUtcDate:yyyy-MM-dd}&dateTo={toUtcDate:yyyy-MM-dd}",
            cancellationToken);

        return (payload?.Matches ?? [])
            .Select(m => new ExternalMatchScore
            {
                ExternalMatchID = m.Id,
                Status = m.Status,
                HomeTeamGoals = m.Score?.FullTime?.Home,
                AwayTeamGoals = m.Score?.FullTime?.Away,
            })
            .ToList();
    }

    /// <summary>
    /// Issues one GET against the provider, having first taken a slot from the shared rate-limit
    /// budget. Every request goes through here rather than each method building its own, so no new
    /// endpoint can accidentally skip the limiter.
    /// </summary>
    /// <typeparam name="T">The response shape to deserialise into.</typeparam>
    /// <param name="relativeUrl">Path and query, relative to the configured base URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken)
    {
        if (!_rateLimiter.TryAcquire())
        {
            throw new ExternalApiRateLimitedException(_rateLimiter.TimeUntilNextSlot());
        }

        var client = _httpClientFactory.CreateClient(nameof(FootballDataApiClient));
        client.DefaultRequestHeaders.Add("X-Auth-Token", _options.Value.ApiKey);

        var response = await client.GetAsync($"{_options.Value.BaseUrl.TrimEnd('/')}/{relativeUrl}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"football-data.org request failed ({(int)response.StatusCode} {response.StatusCode}): {errorBody}");
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Translates the provider's group identifier ("GROUP_A") into the form the site stores and
    /// displays ("Group A"). Returns null where the fixture has no group - a league season's
    /// fixtures, and a tournament's knockout rounds.
    /// </summary>
    /// <param name="group">The provider's group identifier, or null.</param>
    private static string? ToGroupName(string? group)
    {
        if (string.IsNullOrWhiteSpace(group))
        {
            return null;
        }

        // "GROUP_A" -> "Group A". Anything else the provider sends through this field is passed on
        // title-cased rather than dropped, so an unfamiliar format still lands somewhere visible.
        var words = group.Split('_', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length == 1 ? word.ToUpperInvariant() : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant());

        return string.Join(' ', words);
    }

    /// <summary>
    /// Works out the free-text round description to store against a fixture - the group name for a
    /// group-stage fixture, or the round's own name for a knockout one. Null for a league season,
    /// where every fixture is the same kind of fixture and a description adds nothing.
    /// </summary>
    /// <param name="stage">The provider's stage identifier, or null.</param>
    /// <param name="group">The provider's group identifier, or null.</param>
    private static string? ToStageDescription(string? stage, string? group)
    {
        if (stage is not null && KnockoutStageNames.TryGetValue(stage, out var knockoutRound))
        {
            return knockoutRound;
        }

        return ToGroupName(group);
    }

    /// <summary>
    /// Translates the provider's stage identifier into the site's KnockoutRound value, or null for a
    /// stage that isn't a knockout round.
    /// </summary>
    /// <param name="stage">The provider's stage identifier, or null.</param>
    private static int? ToKnockoutRound(string? stage)
    {
        if (stage is not null && KnockoutStageRounds.TryGetValue(stage, out var knockoutRound))
        {
            return knockoutRound;
        }

        return null;
    }

    // Each knockout stage's KnockoutRound value - the number of teams contesting it, except for the
    // third-place play-off, which takes the sentinel. See Application/Common/KnockoutRounds.cs.
    // A play-off round deciding entry to the tournament proper is left out deliberately: it happens
    // before the group stage and is no part of the bracket.
    private static readonly Dictionary<string, int> KnockoutStageRounds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ROUND_OF_32"] = 32,
        ["LAST_32"] = 32,
        ["ROUND_OF_16"] = 16,
        ["LAST_16"] = 16,
        ["QUARTER_FINALS"] = 8,
        ["SEMI_FINALS"] = 4,
        ["THIRD_PLACE"] = KnockoutRounds.ThirdPlacePlayOffRound,
        ["FINAL"] = 2,
    };

    // The provider's knockout stage identifiers, mapped to the round names this site shows. A stage
    // absent from here is treated as non-knockout, which is the right default: it covers a league
    // season's REGULAR_SEASON as well as GROUP_STAGE, and a knockout round wrongly imported as a
    // group fixture is a description an admin can correct, where the reverse would quietly drop
    // real fixtures out of the group tables.
    private static readonly Dictionary<string, string> KnockoutStageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PLAY_OFF_ROUND"] = "Play-off",
        ["PLAYOFFS"] = "Play-off",
        ["PLAYOFF_ROUND"] = "Play-off",
        ["ROUND_OF_32"] = "Round of 32",
        ["LAST_32"] = "Round of 32",
        ["ROUND_OF_16"] = "Round of 16",
        ["LAST_16"] = "Round of 16",
        ["QUARTER_FINALS"] = "Quarter-final",
        ["SEMI_FINALS"] = "Semi-final",
        ["THIRD_PLACE"] = "3rd place playoff",
        ["FINAL"] = "Final",
    };

    // football-data.org reports "SCHEDULED" for fixtures whose broadcaster slot isn't confirmed yet -
    // utcDate is a midnight-UTC placeholder in that case, not a real kickoff time. Every other status
    // (TIMED, IN_PLAY, FINISHED, POSTPONED, etc.) carries a real timestamp.
    private const string ScheduledStatus = "SCHEDULED";

    private class MatchesResponse
    {
        [JsonPropertyName("matches")]
        public List<MatchDto> Matches { get; set; } = [];
    }

    private class MatchDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("utcDate")]
        public DateTime UtcDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        // "stage" and "group" describe where a fixture sits in a tournament - "GROUP_STAGE"/"GROUP_A"
        // for the group phase, "QUARTER_FINALS" and the like once it turns knockout. A league season
        // reports "REGULAR_SEASON" and a null group.
        [JsonPropertyName("stage")]
        public string? Stage { get; set; }

        [JsonPropertyName("group")]
        public string? Group { get; set; }

        [JsonPropertyName("homeTeam")]
        public TeamDto HomeTeam { get; set; } = new();

        [JsonPropertyName("awayTeam")]
        public TeamDto AwayTeam { get; set; } = new();

        [JsonPropertyName("score")]
        public ScoreDto? Score { get; set; }
    }

    private class TeamDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    private class ScoreDto
    {
        // "fullTime" carries the running score while a match is in play, not just the final one -
        // the provider fills it in as goals go in and it settles once the status reaches FINISHED.
        [JsonPropertyName("fullTime")]
        public ScoreLineDto? FullTime { get; set; }
    }

    private class ScoreLineDto
    {
        [JsonPropertyName("home")]
        public int? Home { get; set; }

        [JsonPropertyName("away")]
        public int? Away { get; set; }
    }
}
