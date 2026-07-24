using Microsoft.Extensions.Options;
using Predictathon.Application.Interfaces;
using Predictathon.WebApi.Options;

namespace Predictathon.WebApi.BackgroundServices;

/// <summary>
/// Runs the daily maintenance tasks (prediction reminder emails, league-history snapshots).
/// </summary>
public sealed class ScheduledTasksHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledTasksHostedService> _logger;
    private readonly TimeSpan _runAtUtc;

    /// <summary>
    /// Initialises a new instance of the <see cref="ScheduledTasksHostedService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Used to create a scoped service provider for each run, since the
    /// application services are registered scoped but this hosted service is a singleton.</param>
    /// <param name="options">Supplies the configured daily run time, defaulting to 06:00 UTC.</param>
    /// <param name="logger">Logger used to record failures so a single failing task doesn't take
    /// down the others or the host.</param>
    public ScheduledTasksHostedService(
        IServiceScopeFactory scopeFactory,
        IOptions<ScheduledTasksOptions> options,
        ILogger<ScheduledTasksHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _runAtUtc = options.Value.RunAtUtc;
    }

    /// <summary>
    /// Waits until the next configured run time, executes the scheduled tasks, then repeats until
    /// the host shuts down.
    /// </summary>
    /// <param name="stoppingToken">Signalled when the host is shutting down.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = GetDelayUntilNextRun(DateTime.UtcNow, _runAtUtc);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunTasksAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Calculates how long to wait from <paramref name="utcNow"/> until the next occurrence of
    /// <paramref name="runAtUtc"/>, rolling over to tomorrow if that time has already passed today.
    /// </summary>
    /// <param name="utcNow">The current UTC time.</param>
    /// <param name="runAtUtc">The time of day (UTC) the tasks should run at.</param>
    internal static TimeSpan GetDelayUntilNextRun(DateTime utcNow, TimeSpan runAtUtc)
    {
        var nextRun = utcNow.Date + runAtUtc;
        if (nextRun <= utcNow)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - utcNow;
    }

    /// <summary>
    /// Runs each scheduled task in its own scope, logging and continuing past any individual
    /// failure so one broken task doesn't block the others.
    /// </summary>
    /// <param name="cancellationToken">Propagated to the task methods that accept one.</param>
    private async Task RunTasksAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        try
        {
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            await userService.SendPredictionEmailRemindersAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled prediction email reminder run failed.");
        }

        try
        {
            var competitionService = scope.ServiceProvider.GetRequiredService<ICompetitionService>();
            await competitionService.SetUserCompetitionLeagueHistoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled league-history snapshot run failed.");
        }
    }
}
