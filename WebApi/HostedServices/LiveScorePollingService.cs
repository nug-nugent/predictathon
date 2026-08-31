using Predictathon.Application.Interfaces;

namespace Predictathon.WebApi.HostedServices;

/// <summary>
/// Drives <see cref="ILiveScoreService"/> on a schedule the service itself decides: fast while
/// matches are in play, otherwise asleep until the next kick-off. Deliberately nothing more than a
/// loop - all the logic lives in the Application layer, so this can be tested without a host and
/// replaced with an external trigger if the hosting ever demands it.
///
/// An in-process timer is normally the wrong shape on this app's shared IIS hosting, which is why
/// the daily maintenance jobs are external pings against TasksController instead. Live scores are
/// the exception, because the data has exactly one consumer - somebody looking at the site - and
/// looking at the site is what starts the process:
///
///  - A recycle brings a fresh worker up on its own, so the loop resumes without help.
///  - An idle shutdown stops the worker until the next request, but the first request is also the
///    first time anyone wants a score. The loop refreshes immediately on start rather than after an
///    interval, so a cold start costs a second or two rather than a full poll cycle.
///  - While anyone is actually watching, the Live page's own 30-second polling keeps the app pool
///    alive, so the loop keeps running for as long as it's wanted.
///
/// The one case that isn't self-correcting is an overlapped recycle briefly running two workers,
/// each with its own rate limiter; LiveScoreService's stored poll timestamp is what settles that.
/// </summary>
public class LiveScorePollingService : BackgroundService
{
    /// <summary>How long to wait after an unexpected failure before trying again.</summary>
    private static readonly TimeSpan ErrorBackoff = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LiveScorePollingService> _logger;

    /// <summary>
    /// Initialises a new instance of the <see cref="LiveScorePollingService"/> class.
    /// </summary>
    /// <param name="scopeFactory">Creates a scope per pass, since the live-score service and its DbContext are scoped.</param>
    /// <param name="logger">Logs each pass's outcome.</param>
    public LiveScorePollingService(IServiceScopeFactory scopeFactory, ILogger<LiveScorePollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Live score polling started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = await RunPassAsync(stoppingToken);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown, not a fault - fall out of the loop quietly.
                break;
            }
        }

        _logger.LogInformation("Live score polling stopped.");
    }

    /// <summary>
    /// Runs one refresh and works out how long to wait before the next, returning the error backoff
    /// instead if the pass failed. Never throws: an exception escaping here would end the loop for
    /// the lifetime of the process, and the next worker wouldn't start until someone made a request.
    /// </summary>
    /// <param name="stoppingToken">Cancellation token.</param>
    private async Task<TimeSpan> RunPassAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var liveScoreService = scope.ServiceProvider.GetRequiredService<ILiveScoreService>();

            var summary = await liveScoreService.RefreshAsync(stoppingToken);

            if (summary.ScoresChanged > 0)
            {
                _logger.LogInformation(
                    "Live scores updated for {ScoresChanged} of {MatchesInPlay} matches in play.",
                    summary.ScoresChanged, summary.MatchesInPlay);
            }

            return await liveScoreService.GetNextRefreshDelayAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return TimeSpan.Zero;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Live score refresh failed.");
            return ErrorBackoff;
        }
    }
}
