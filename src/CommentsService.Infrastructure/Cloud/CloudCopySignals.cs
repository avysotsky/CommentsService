using CommentsService.Application.Cloud;
using CommentsService.Application.Events;
namespace CommentsService.Infrastructure.Cloud;

public sealed class CloudCopySignals(CloudSettings settings) : ICloudCopySignals, ICommentCreatedHandler
{
    private readonly object gate = new();
    private readonly Queue<Guid> pending = new();
    private readonly HashSet<Guid> active = [];
    public IReadOnlyList<Guid> ReplayIds => settings.Enabled ? settings.ReplayCommentIds : [];
    public void Handle(CommentCreated notification)
    {
        if (!settings.Enabled || !settings.AllowedCommentIds.Contains(notification.CommentId)) return;
        lock (gate)
        {
            if (active.Count >= settings.Capacity || !active.Add(notification.CommentId)) return;
            pending.Enqueue(notification.CommentId);
        }
    }
    public bool TryTake(out Guid commentId) { lock (gate) return pending.TryDequeue(out commentId); }
    public void Complete(Guid commentId) { lock (gate) active.Remove(commentId); }
}
