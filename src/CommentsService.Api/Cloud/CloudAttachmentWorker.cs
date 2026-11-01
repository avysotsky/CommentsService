using CommentsService.Application.Cloud;
namespace CommentsService.Api.Cloud;

public sealed class CloudAttachmentWorker(IServiceScopeFactory scopes, ICloudCopySignals signals,
    ILogger<CloudAttachmentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken token)
    {
        // Replay is an explicit bounded list, never a scan or durable-delivery claim.
        foreach (var id in signals.ReplayIds) await CopyAsync(id, token);
        while (!token.IsCancellationRequested)
        {
            if (signals.TryTake(out var id))
            {
                try { await CopyAsync(id, token); } finally { signals.Complete(id); }
            }
            else await Task.Delay(100, token);
        }
    }
    private async Task CopyAsync(Guid id, CancellationToken token)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var attachment = await scope.ServiceProvider.GetRequiredService<ICloudAttachmentSource>().FindByCommentAsync(id, token);
            if (attachment is not null) await scope.ServiceProvider.GetRequiredService<IAttachmentCloud>().CopyAsync(attachment, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception) { logger.LogWarning("Cloud copy job failed; SQL originals remain available"); }
    }
}
