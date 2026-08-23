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

    private readonly IGenericDbContext _dbContext;
    private readonly IAvatarService _avatarService;

    public StatisticsService(IGenericDbContext dbContext, IAvatarService avatarService)
    {
        _dbContext = dbContext;
        _avatarService = avatarService;
    }

    // A DbContext (and the ADO.NET connection it wraps) isn't safe for concurrent operations, so
    // these calls run sequentially on the one scoped context rather than via Task.WhenAll.
    /// <inheritdoc />
    public async Task<AllTimeStatisticsModel> GetAllTimeStatisticsAsync(CancellationToken cancellationToken = default)
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
    public async Task<IReadOnlyList<LeagueTableItem>> GetAllTimeLeagueTableAsync(CancellationToken cancellationToken = default)
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
