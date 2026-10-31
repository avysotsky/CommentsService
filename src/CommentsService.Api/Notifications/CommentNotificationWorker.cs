using CommentsService.Application.Notifications;
using CommentsService.Infrastructure.Notifications;

namespace CommentsService.Api.Notifications;

public sealed class CommentNotificationWorker(CommentNotificationQueue queue, NotificationSettings settings,
    ICommentChangeDelivery delivery, ILogger<CommentNotificationWorker> logger) : BackgroundService
{
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop can race with BackgroundService's first scheduled ExecuteAsync call.
        queue.Complete();
        await base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var hint in queue.Reader.ReadAllAsync(stoppingToken))
            {
                for (var attempt = 1; attempt <= settings.MaximumAttempts; attempt++)
                {
                    try { await delivery.DeliverAsync(stoppingToken); break; }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                    catch (Exception)
                    {
                        logger.LogWarning("Comments refresh delivery failed on attempt {Attempt} of {MaximumAttempts}", attempt, settings.MaximumAttempts);
                        if (attempt < settings.MaximumAttempts)
                            await Task.Delay(settings.RetryDelayMilliseconds, stoppingToken);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            queue.Complete();
            logger.LogInformation("Comments notification worker stopped; {Coalesced} pending hints were coalesced", queue.Coalesced);
        }
    }
}
