using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Predictathon.Application.Attributes;
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
    private const string BaseUrl = "https://api.football-data.org/v4";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<FootballDataApiOptions> _options;

    public FootballDataApiClient(IHttpClientFactory httpClientFactory, IOptions<FootballDataApiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExternalFixture>> GetFixturesAsync(string competitionCode, int season, CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient(nameof(FootballDataApiClient));
        client.DefaultRequestHeaders.Add("X-Auth-Token", _options.Value.ApiKey);

        var response = await client.GetAsync($"{BaseUrl}/competitions/{competitionCode}/matches?season={season}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<MatchesResponse>(cancellationToken: cancellationToken);

        return (payload?.Matches ?? [])
            .Select(m => new ExternalFixture
            {
                ExternalMatchID = m.Id,
                KickoffUtc = m.UtcDate,
                HomeTeamExternalCode = m.HomeTeam.Id.ToString(),
                AwayTeamExternalCode = m.AwayTeam.Id.ToString(),
                HomeTeamName = m.HomeTeam.Name,
                AwayTeamName = m.AwayTeam.Name,
            })
            .ToList();
    }

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

        [JsonPropertyName("homeTeam")]
        public TeamDto HomeTeam { get; set; } = new();

        [JsonPropertyName("awayTeam")]
        public TeamDto AwayTeam { get; set; } = new();
    }

    private class TeamDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }
}
