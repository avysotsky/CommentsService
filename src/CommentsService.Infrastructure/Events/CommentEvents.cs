using CommentsService.Application.Events;
using Microsoft.Extensions.Logging;

namespace CommentsService.Infrastructure.Events;

public sealed class CommentEvents(IEnumerable<ICommentCreatedHandler> handlers, ILogger<CommentEvents> logger) : ICommentEvents
{
    public void Publish(CommentCreated notification)
    {
        foreach (var handler in handlers)
        {
            try { handler.Handle(notification); }
            catch (Exception)
            {
                // Do not log exception messages that might contain content or provider secrets.
                logger.LogWarning("Comment event {EventId} subscriber {Subscriber} failed after commit",
                    notification.EventId, handler.GetType().Name);
            }
        }
    }
}
