using Microsoft.Data.SqlClient;
using Predictathon.Application.Attributes;
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

    public StatisticsService(IGenericDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // A DbContext (and the ADO.NET connection it wraps) isn't safe for concurrent operations, so
    // these calls run sequentially on the one scoped context rather than via Task.WhenAll.
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

    public async Task<CurrentCompetitionStatisticsModel> GetCurrentCompetitionStatisticsAsync(Guid competitionId, Guid userId, CancellationToken cancellationToken = default)
    {
        var predictableTeams = await _dbContext.CallStoredProcedureAsync<PredictableTeamListItem>(
            "AverageScoreByTeamListGet",
            [new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId }],
            cancellationToken);

        var matchResults = await _dbContext.CallStoredProcedureAsync<PredictableMatchListItem>(
            "MatchResultListGet",
            [
                new SqlParameter("@UserID", SqlDbType.UniqueIdentifier) { Value = userId },
                new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId },
            ],
            cancellationToken);

        var bestPredictions = await _dbContext.CallStoredProcedureAsync<BestPredictionListItem>(
            "MatchPredictionAverageBiggestDifferencesGet",
            [new SqlParameter("@CompetitionID", SqlDbType.UniqueIdentifier) { Value = competitionId }],
            cancellationToken);

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
}
