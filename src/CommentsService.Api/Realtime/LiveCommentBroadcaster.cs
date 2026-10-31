using System.Threading.Channels;
using CommentsService.Application.Notifications;

namespace CommentsService.Api.Realtime;

// No network I/O on the notification worker: each client has one coalescing slot.
public sealed class LiveCommentBroadcaster(RealtimeSettings settings) : ICommentChangeDelivery
{
    private readonly object gate = new();
    private readonly HashSet<Subscription> clients = [];

    public Subscription? TrySubscribe()
    {
        lock (gate)
        {
            if (clients.Count >= settings.MaximumConnections) return null;
            var subscription = new Subscription(this);
            clients.Add(subscription); return subscription;
        }
    }

    public Task DeliverAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate) foreach (var client in clients) client.Pending.Writer.TryWrite(0);
        return Task.CompletedTask;
    }

    public sealed class Subscription : IDisposable
    {
        private readonly LiveCommentBroadcaster owner;
        internal readonly Channel<byte> Pending = Channel.CreateBounded<byte>(new BoundedChannelOptions(1)
        { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, AllowSynchronousContinuations = false });
        public ChannelReader<byte> Reader => Pending.Reader;
        internal Subscription(LiveCommentBroadcaster owner) => this.owner = owner;
        public void Dispose()
        {
            lock (owner.gate) { owner.clients.Remove(this); Pending.Writer.TryComplete(); }
        }
    }
}
