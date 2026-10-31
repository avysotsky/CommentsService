using CommentsService.Application.Search;
using CommentsService.Infrastructure.Search;
namespace CommentsService.Api.Search;
public sealed class CommentSearchWorker(SearchSettings settings, SearchSignals signals, IServiceScopeFactory scopes, ILogger<CommentSearchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        if (!settings.Enabled) return;
        Guid? cursor = null; bool cleanup = false; var nextScan = DateTimeOffset.MinValue;
        while (!token.IsCancellationRequested)
        {
            var delay = settings.BatchDelayMilliseconds;
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var source = scope.ServiceProvider.GetRequiredService<ICommentSearchSource>();
                var index = scope.ServiceProvider.GetRequiredService<ICommentSearchIndex>();
                var ids = signals.Drain(settings.BatchSize);
                if (ids.Length > 0) await index.IndexAsync(await source.GetAsync(ids,token),token);
                if (DateTimeOffset.UtcNow >= nextScan)
                {
                    if (!cleanup)
                    {
                        var batch = await source.ScanAsync(cursor,settings.BatchSize,token);
                        await index.IndexAsync(batch,token);
                        if (batch.Count < settings.BatchSize) { cursor=null; cleanup=true; } else cursor=batch[^1].Id;
                    }
                    else
                    {
                        var batch = await index.ScanIdsAsync(cursor,settings.BatchSize,token);
                        var live = (await source.GetAsync(batch,token)).Select(d=>d.Id).ToHashSet();
                        await index.DeleteAsync(batch.Where(id=>!live.Contains(id)).ToArray(),token);
                        if (batch.Count < settings.BatchSize) { cursor=null; cleanup=false; nextScan=DateTimeOffset.UtcNow.AddSeconds(settings.ReconcileIntervalSeconds); }
                        else cursor=batch[^1];
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Search indexing unavailable; existing application functions remain active"); delay=settings.FailureRetrySeconds*1000; }
            try { await Task.Delay(delay,token); } catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
        }
    }
}
