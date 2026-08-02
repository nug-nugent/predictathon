using Microsoft.EntityFrameworkCore;
using Predictathon.Domain.Entities;

namespace Predictathon.Application.Interfaces.Persistence;

public interface IApplicationDbContext : IGenericDbContext
{
    DbSet<Competition> Competition { get; }
    DbSet<ErrorLog> ErrorLog { get; }
    DbSet<FixtureChangeProposal> FixtureChangeProposal { get; }
    DbSet<HallOfFame> HallOfFame { get; }
    DbSet<Match> Match { get; }
    DbSet<Message> Message { get; }
    DbSet<MessageReaction> MessageReaction { get; }
    DbSet<MessageThread> MessageThread { get; }
    DbSet<MessageThreadRead> MessageThreadRead { get; }
    DbSet<PaymentCredit> PaymentCredit { get; }
    DbSet<Prediction> Prediction { get; }
    DbSet<PredictionHistory> PredictionHistory { get; }
    DbSet<Team> Team { get; }
    DbSet<TeamCompetition> TeamCompetition { get; }
    DbSet<Transaction> Transaction { get; }
    DbSet<UserCompetition> UserCompetition { get; }
    DbSet<UserCompetitionLeagueHistory> UserCompetitionLeagueHistory { get; }
}