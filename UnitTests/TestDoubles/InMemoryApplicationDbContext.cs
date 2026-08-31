using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Predictathon.Application.Interfaces.Persistence;
using Predictathon.Domain.Identity;
using System.Linq.Expressions;
using Entities = Predictathon.Domain.Entities;

namespace Predictathon.UnitTests.TestDoubles;

/// <summary>
/// EF Core InMemory-backed stand-in for <see cref="IApplicationDbContext"/>, so validators and
/// services that query <c>DbSet{T}</c> properties directly can be exercised without a real SQL
/// Server instance. Stored-procedure calls aren't supported by the InMemory provider and are
/// stubbed out - tests that depend on their results should assert on the surrounding logic only.
/// </summary>
public class InMemoryApplicationDbContext : DbContext, IApplicationDbContext
{
    public InMemoryApplicationDbContext()
        : base(new DbContextOptionsBuilder<InMemoryApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options)
    {
    }

    public DbSet<Entities.Announcement> Announcement => Set<Entities.Announcement>();
    public DbSet<Entities.Competition> Competition => Set<Entities.Competition>();
    public DbSet<Entities.CompetitionSeries> CompetitionSeries => Set<Entities.CompetitionSeries>();
    public DbSet<Entities.ErrorLog> ErrorLog => Set<Entities.ErrorLog>();
    public DbSet<Entities.FixtureChangeProposal> FixtureChangeProposal => Set<Entities.FixtureChangeProposal>();
    public DbSet<Entities.HallOfFame> HallOfFame => Set<Entities.HallOfFame>();
    public DbSet<Entities.Match> Match => Set<Entities.Match>();
    public DbSet<Entities.MatchLiveScore> MatchLiveScore => Set<Entities.MatchLiveScore>();
    public DbSet<Entities.Message> Message => Set<Entities.Message>();
    public DbSet<Entities.MessageReaction> MessageReaction => Set<Entities.MessageReaction>();
    public DbSet<Entities.MessageThread> MessageThread => Set<Entities.MessageThread>();
    public DbSet<Entities.MessageThreadRead> MessageThreadRead => Set<Entities.MessageThreadRead>();
    public DbSet<Entities.PaymentCredit> PaymentCredit => Set<Entities.PaymentCredit>();
    public DbSet<Entities.Prediction> Prediction => Set<Entities.Prediction>();
    public DbSet<Entities.PredictionHistory> PredictionHistory => Set<Entities.PredictionHistory>();
    public DbSet<Entities.Team> Team => Set<Entities.Team>();
    public DbSet<Entities.TeamCompetition> TeamCompetition => Set<Entities.TeamCompetition>();
    public DbSet<Entities.Transaction> Transaction => Set<Entities.Transaction>();
    public DbSet<Entities.UserCompetition> UserCompetition => Set<Entities.UserCompetition>();
    public DbSet<Entities.UserCompetitionLeagueHistory> UserCompetitionLeagueHistory => Set<Entities.UserCompetitionLeagueHistory>();

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Navigations kept out of the blanket-strip below because specific service tests query through
    // them - PaymentCreditService.GetAllAsync (ForCompetition), MessageboardService.GetMessagesAsync
    // (.Include(m => m.MessageReaction)), MatchService.GetForAdminAsync (.HomeTeam.TeamName) and
    // LiveScoreService (.Competition.ExternalApiCompetitionCode, .MatchLiveScore).
    private static readonly HashSet<(Type EntityType, string PropertyName)> PreservedNavigations =
    [
        (typeof(Entities.PaymentCredit), nameof(Entities.PaymentCredit.ForCompetition)),
        (typeof(Entities.Message), nameof(Entities.Message.MessageReaction)),
        (typeof(Entities.Match), nameof(Entities.Match.HomeTeam)),
        (typeof(Entities.Match), nameof(Entities.Match.Competition)),
        (typeof(Entities.Match), nameof(Entities.Match.MatchLiveScore)),
        (typeof(Entities.MatchLiveScore), nameof(Entities.MatchLiveScore.Match)),
    ];

    /// <summary>
    /// Strips out navigation properties (references and collections onto other Domain entities) so
    /// EF's InMemory provider doesn't need the full web of relationships configured - only the
    /// scalar/FK columns the validators and services under test actually query on, plus anything
    /// listed in <see cref="PreservedNavigations"/>.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;
            foreach (var property in clrType.GetProperties())
            {
                var propertyType = property.PropertyType;
                var isCollectionNavigation = propertyType.IsGenericType
                    && propertyType.GetGenericTypeDefinition() == typeof(ICollection<>);
                var isReferenceNavigation = propertyType.Namespace == "Predictathon.Domain.Entities"
                    && propertyType != clrType;

                if ((isCollectionNavigation || isReferenceNavigation) && !PreservedNavigations.Contains((clrType, property.Name)))
                {
                    modelBuilder.Entity(clrType).Ignore(property.Name);
                }
            }
        }

        modelBuilder.Entity<Entities.PaymentCredit>()
            .HasOne(p => p.ForCompetition)
            .WithMany()
            .HasForeignKey(p => p.ForCompetitionID);

        modelBuilder.Entity<Entities.Message>()
            .HasMany(m => m.MessageReaction)
            .WithOne()
            .HasForeignKey(r => r.MessageID);

        // This model is built from scratch rather than inherited from ApplicationDbContext, so
        // anything convention can't work out for itself has to be repeated here. Every other entity
        // gets away with it because its key is named <Type>ID; MatchLiveScore is keyed on the match
        // it hangs off, which convention doesn't recognise, and the one-to-one back to Match needs
        // saying out loud or EF invents a shadow foreign key that doesn't line up with MatchID.
        modelBuilder.Entity<Entities.MatchLiveScore>(entity =>
        {
            entity.HasKey(e => e.MatchID);
            entity.HasOne(e => e.Match)
                .WithOne(m => m.MatchLiveScore)
                .HasForeignKey<Entities.MatchLiveScore>(e => e.MatchID);
        });
    }

    public IQueryable<T> Query<T>() where T : class => Set<T>();

    public async Task<T?> GetByIdAsync<T>(object id, CancellationToken cancellationToken = default) where T : class
        => await Set<T>().FindAsync([id], cancellationToken);

    public Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) where T : class
        => Set<T>().FirstOrDefaultAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<T>> ListAsync<T>(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default) where T : class
    {
        var query = Set<T>().AsQueryable();
        if (predicate is not null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public new async Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
        => await Set<T>().AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
        => await Set<T>().AddRangeAsync(entities, cancellationToken);

    public new void Update<T>(T entity) where T : class => Set<T>().Update(entity);

    public void UpdateRange<T>(IEnumerable<T> entities) where T : class => Set<T>().UpdateRange(entities);

    public new void Remove<T>(T entity) where T : class => Set<T>().Remove(entity);

    public void RemoveRange<T>(IEnumerable<T> entities) where T : class => Set<T>().RemoveRange(entities);

    public Task ExecuteSqlAsync(string sql, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Raw SQL execution isn't supported against the InMemory test double.");

    public Task CallStoredProcedureAsync(string storedProcedureName, List<SqlParameter>? parameters = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<List<TReturnType>> CallStoredProcedureAsync<TReturnType>(string storedProcedureName, List<SqlParameter>? parameters = null, CancellationToken cancellationToken = default)
        where TReturnType : class, new()
        => Task.FromResult(new List<TReturnType>());
}
