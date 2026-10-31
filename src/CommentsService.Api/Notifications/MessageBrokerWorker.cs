using CommentsService.Infrastructure.Broker;

namespace CommentsService.Api.Notifications;

public sealed class MessageBrokerWorker(BrokerSettings settings, BrokerNotificationRouter router,
    RabbitCommentBroker transport, ILogger<MessageBrokerWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => !settings.Enabled ? Task.CompletedTask :
        Task.WhenAll(ConnectionsAsync(stoppingToken), DeadlinesAsync(stoppingToken));

    private async Task ConnectionsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try { await transport.RunSessionAsync(token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Broker unavailable; local notifications remain active"); }
            finally { router.SetConnected(false); }
            try { await Task.Delay(settings.ReconnectDelayMilliseconds, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
        }
    }

    private async Task DeadlinesAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        try { while (await timer.WaitForNextTickAsync(token)) router.Expire(); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        router.Complete();
        await base.StopAsync(cancellationToken);
        var stats = router.Statistics;
        logger.LogInformation("Broker stopped; received {Received}, confirmed {Confirmed}, local fallback {Fallback}", stats.Received, stats.Confirmed, stats.Fallback);
    }
}
