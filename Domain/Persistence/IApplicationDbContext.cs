using Microsoft.EntityFrameworkCore;
using Predictathon.Domain.Entities;

namespace Predictathon.Domain.Persistence;

public interface IApplicationDbContext
{
    DbSet<Competition> Competition { get; }
    DbSet<HallOfFame> HallOfFame { get; }
    DbSet<Match> Match { get; }
    DbSet<Message> Message { get; }
    DbSet<MessageReaction> MessageReaction { get; }
    DbSet<MessageThread> MessageThread { get; }
    DbSet<PaymentCredit> PaymentCredit { get; }
    DbSet<Prediction> Prediction { get; }
    DbSet<PredictionHistory> PredictionHistory { get; }
    DbSet<Team> Team { get; }
    DbSet<TeamCompetition> TeamCompetition { get; }
    DbSet<Transaction> Transaction { get; }
    DbSet<User> User { get; }
    DbSet<UserCompetition> UserCompetition { get; }
    DbSet<UserCompetitionLeagueHistory> UserCompetitionLeagueHistory { get; }
}