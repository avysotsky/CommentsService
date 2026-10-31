using CommentsService.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace CommentsService.Infrastructure.Notifications;

public sealed class LoggedCommentChangeDelivery(ILogger<LoggedCommentChangeDelivery> logger) : ICommentChangeDelivery
{
    public Task DeliverAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogDebug("Queued comments refresh delivered");
        return Task.CompletedTask;
    }
}
