using Microsoft.Extensions.Options;
using Predictathon.Application.Interfaces;
using Predictathon.WebApi.Options;

namespace Predictathon.WebApi.BackgroundServices;

/// <summary>
/// Periodically checks Premier League fixtures against the external data source for reschedules,
/// raising pending <see cref="Domain.Entities.FixtureChangeProposal"/> rows for an admin to review.
/// Runs on its own interval (rather than once a day like <see cref="ScheduledTasksHostedService"/>),
/// since broadcasters can reschedule a fixture at any time of day.
/// </summary>
public sealed class FixtureSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FixtureSyncHostedService> _logger;
    private readonly TimeSpan _interval;

    /// <summary>
    /// Initialises a new instance of the <see cref="FixtureSyncHostedService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Used to create a scoped service provider for each run, since the
    /// application services are registered scoped but this hosted service is a singleton.</param>
    /// <param name="options">Supplies the configured run interval, defaulting to every 4 hours.</param>
    /// <param name="logger">Logger used to record failures so a bad run doesn't crash the host.</param>
    public FixtureSyncHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<FixtureSyncOptions> options,
        ILogger<FixtureSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromHours(options.Value.IntervalHours);
    }

    /// <summary>
    /// Runs a fixture-change detection pass, then waits the configured interval, repeating until
    /// the host shuts down.
    /// </summary>
    /// <param name="stoppingToken">Signalled when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                try
                {
                    var fixtureChangeProposalService = scope.ServiceProvider.GetRequiredService<IFixtureChangeProposalService>();
                    await fixtureChangeProposalService.DetectChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fixture sync run failed.");
                }
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
