using System.Threading.Channels;
using CommentsService.Application.Events;

namespace CommentsService.Infrastructure.Notifications;

public sealed class CommentNotificationQueue : ICommentCreatedHandler
{
    private readonly Channel<byte> channel;
    private long coalesced;
    public long Coalesced => Interlocked.Read(ref coalesced);
    public ChannelReader<byte> Reader => channel.Reader;

    public CommentNotificationQueue(NotificationSettings settings)
    {
        channel = Channel.CreateBounded<byte>(new BoundedChannelOptions(settings.Capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        }, _ => Interlocked.Increment(ref coalesced));
    }

    // All hints mean "refresh the current view". Keeping the latest covers dropped hints too.
    public void Handle(CommentCreated notification) => channel.Writer.TryWrite(0);
    public void Complete() => channel.Writer.TryComplete();
}
