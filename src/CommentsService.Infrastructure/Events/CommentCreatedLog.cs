using CommentsService.Application.Events;
using Microsoft.Extensions.Logging;

namespace CommentsService.Infrastructure.Events;

public sealed class CommentCreatedLog(ILogger<CommentCreatedLog> logger) : ICommentCreatedHandler
{
    public void Handle(CommentCreated notification) =>
        logger.LogInformation("Comment committed {EventId} {CommentId} {ParentId} {RootId}",
            notification.EventId, notification.CommentId, notification.ParentId, notification.RootId);
}
