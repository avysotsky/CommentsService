using CommentsService.Infrastructure.Broker;
using CommentsService.Infrastructure.Search;
namespace CommentsService.Api.Search;
public sealed class SearchBrokerWorker(SearchSettings settings, BrokerSettings broker, RabbitSearchSubscriber subscriber, ILogger<SearchBrokerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        if (!settings.Enabled || !broker.Enabled) return;
        while (!token.IsCancellationRequested)
        {
            try { await subscriber.RunSessionAsync(token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Search broker subscription unavailable; SQL reconciliation remains active"); }
            try { await Task.Delay(broker.ReconnectDelayMilliseconds,token); } catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
        }
    }
}
