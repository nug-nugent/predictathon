using Microsoft.EntityFrameworkCore;
using Predictathon.Infrastructure.Persistence;

namespace Predictathon.IntegrationTests;

/// <summary>
/// Shared connection setup for integration tests that exercise real SQL Server logic (stored
/// procedures, computed aggregates) that isn't meaningfully testable via EF's InMemory provider or
/// a hand-rolled fake - see UnitTests for the LINQ-friendly layers. Requires a migrated (SSDT-deployed)
/// database; the seed data itself isn't required, as each test creates and cleans up its own rows.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    public const string ConnectionStringEnvironmentVariable = "PREDICTATHON_TEST_CONNECTION";

    public string ConnectionString { get; } = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)
        ?? throw new InvalidOperationException(
            $"Set the {ConnectionStringEnvironmentVariable} environment variable to a connection string for a " +
            "migrated Predictathon database before running IntegrationTests. Examples:\n" +
            "  Docker dev stack (docker compose --env-file .env.docker up -d db db-migrate):\n" +
            "    Server=localhost,14330;Database=Predictathon;User Id=sa;Password=<MSSQL_SA_PASSWORD from .env.docker>;TrustServerCertificate=true\n" +
            "  Native workflow ((local) SQL Server, schema published from Database/Predictathon.Database.csproj):\n" +
            "    Server=(local);Database=Predictathon;Trusted_Connection=true;TrustServerCertificate=true");

    /// <summary>
    /// Creates a new <see cref="ApplicationDbContext"/> against the test database. Callers own the
    /// returned context and should dispose it (e.g. via <c>await using</c>).
    /// </summary>
    public ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await using var dbContext = CreateDbContext();
        var canConnect = await dbContext.Database.CanConnectAsync();
        if (!canConnect)
        {
            throw new InvalidOperationException(
                $"Could not connect to the integration test database using the connection string from " +
                $"{ConnectionStringEnvironmentVariable}. Is the database running and migrated? " +
                "See the 'Docker dev stack' / 'Native workflow' commands in CLAUDE.md.");
        }
    }

    /// <inheritdoc />
    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string Name = "Database";
}
