using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Predictathon.Application.Attributes;
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
