using Microsoft.Data.SqlClient;
using Predictathon.Application.Attributes;
using Predictathon.Application.Extensions;
using Predictathon.Application.Interfaces;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Application.Models;
using System.Data;

namespace Predictathon.Application.Services;

[ScopedService]
public class StatisticsService : IStatisticsService
{
    // MatchResultListGet has no built-in row limit (unlike the Statistics_* procedures, which are
    // all TOP 10), so the most/least predictable match lists are capped here in code - matching the
    // legacy PredictableMatches control's MaxResults="50" behaviour.
    private const int PredictableMatchesMaxResults = 50;

    /// <summary>
    /// How long the all-time aggregates are reused for. Longer than a competition's own table,
    /// because these are the reads that grow without bound - they cover every user against every
    /// match ever played, with no competition filter and no date range - while changing no more
    /// often, since a result is a result wherever it lands. As with the league tables the lifetime
    /// is only a backstop; processing a result drops them outright.
    /// </summary>
    private static readonly TimeSpan AllTimeLifetime = TimeSpan.FromMinutes(10);

    private readonly IGenericDbContext _dbContext;
    private readonly IAvatarService _avatarService;
    private readonly ILeagueDataCache _cache;

    public StatisticsService(IGenericDbContext dbContext, IAvatarService avatarService, ILeagueDataCache cache)
    {
        _dbContext = dbContext;
        _avatarService = avatarService;
        _cache = cache;
    }

    // A DbContext (and the ADO.NET connection it wraps) isn't safe for concurrent operations, so
    // these calls run sequentially on the one scoped context rather than via Task.WhenAll.
    /// <inheritdoc />
    /// <remarks>
    /// Five stored procedures, each aggregating over every prediction ever made, behind one cached
    /// entry - so a visit to the Statistics page costs five queries when it's cold and none when
    /// it isn't. The page takes no user context, so everybody's answer is the same one.
    /// </remarks>
    public async Task<AllTimeStatisticsModel> GetAllTimeStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAllTimeAsync(
            "statistics:all-time",
            () => LoadAllTimeStatisticsAsync(cancellationToken),
            AllTimeLifetime,
            cancellationToken);
    }

    /// <summary>
    /// Runs the five all-time statistics procedures.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<AllTimeStatisticsModel> LoadAllTimeStatisticsAsync(CancellationToken cancellationToken)
    {
        return new AllTimeStatisticsModel
        {
            CompetitionWinners = await _dbContext.CallStoredProcedureAsync<CompetitionWinnerListItem>("Statistics_CompetitionWinnerListGet", cancellationToken: cancellationToken),
            HighestAllTimeScores = await _dbContext.CallStoredProcedureAsync<HighestAllTimeScoreListItem>("Statistics_HighestAllTimeScoreListGet", cancellationToken: cancellationToken),
            HighestAverageScores = await _dbContext.CallStoredProcedureAsync<HighestAverageScoreListItem>("Statistics_HighestAverageScorePerPredictionsGet", cancellationToken: cancellationToken),
            HighestPercentageCorrect = await _dbContext.CallStoredProcedureAsync<HighestPercentageCorrectListItem>("Statistics_HighestPercentageCorrectPredictionsGet", cancellationToken: cancellationToken),
            MostPredictions = await _dbContext.CallStoredProcedureAsync<MostPredictionsListItem>("Statistics_MostMatchesPredictedUserListGet", cancellationToken: cancellationToken),
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// The single query most likely to slow down as seasons accumulate: every registered user
    /// crossed with every played match they could have predicted, across the whole history of the
    /// competition, with nothing bounding it. Cached for the same reason and dropped by the same
    /// signal as everything else here.
    /// </remarks>
    public async Task<IReadOnlyList<LeagueTableItem>> GetAllTimeLeagueTableAsync(CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAllTimeAsync(
            "statistics:all-time-league-table",
            () => LoadAllTimeLeagueTableAsync(cancellationToken),
            AllTimeLifetime,
            cancellationToken);
    }

    /// <summary>
    /// Runs the all-time league table and stamps avatar URLs onto it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task<IReadOnlyList<LeagueTableItem>> LoadAllTimeLeagueTableAsync(CancellationToken cancellationToken)
    {
        var table = await _dbContext.CallStoredProcedureAsync<LeagueTableItem>("Statistics_AllTimeLeagueTableGet", cancellationToken: cancellationToken);

        return table.WithAvatarUrls(_avatarService);
    }

    /// <inheritdoc />
    public async Task<CurrentCompetitionStatisticsModel> GetCurrentCompetitionStatisticsAsync(Guid competitionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var predictableTeams = await _dbContext.CallStoredProcedureAsync<PredictableTeamListItem>(
            "AverageScoreByTeamListGet",
            [new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId }],
            cancellationToken);

        var matchResults = await _dbContext.CallStoredProcedureAsync<MatchListItem>(
            "MatchResultListGet",
            [
                new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
                new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            ],
            cancellationToken);

        var bestPredictions = await GetBestPredictionsAsync(competitionId, cancellationToken: cancellationToken);

        return new CurrentCompetitionStatisticsModel
        {
            PredictableTeams = predictableTeams,
            MostPredictableMatches = matchResults
                .OrderByDescending(m => m.AveragePredictionScore)
                .Take(PredictableMatchesMaxResults)
                .ToList(),
            LeastPredictableMatches = matchResults
                .OrderBy(m => m.AveragePredictionScore)
                .Take(PredictableMatchesMaxResults)
                .ToList(),
            BestPredictions = bestPredictions,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BestPredictionListItem>> GetBestPredictionsAsync(Guid competitionId, DateOnly? dateFrom = null, DateOnly? dateTo = null, CancellationToken cancellationToken = default)
    {
        return await _dbContext.CallStoredProcedureAsync<BestPredictionListItem>(
            "MatchPredictionAverageBiggestDifferencesGet",
            [
                new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
                new SqlParameter("@DateFrom", SqlDbType.Date) { Value = ToSqlValue(dateFrom) },
                new SqlParameter("@DateTo", SqlDbType.Date) { Value = ToSqlValue(dateTo) },
            ],
            cancellationToken);
    }

    private static object ToSqlValue(DateOnly? date)
        => date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
}
