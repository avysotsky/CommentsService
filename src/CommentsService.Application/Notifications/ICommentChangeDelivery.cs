namespace CommentsService.Application.Notifications;

// A hint to re-read authoritative REST data, never a persisted comment payload.
public interface ICommentChangeDelivery
{
    Task DeliverAsync(CancellationToken cancellationToken);
}
