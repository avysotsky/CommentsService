using System.Threading.Channels;
using CommentsService.Application.Events;
using CommentsService.Infrastructure.Caching;
using CommentsService.Infrastructure.Notifications;

namespace CommentsService.Infrastructure.Broker;

// Disposable refresh hints only. No database/network work or task-per-event in the synchronous subscriber.
public sealed class BrokerNotificationRouter(BrokerSettings settings, CommentNotificationQueue local, CommentReadCache cache, TimeProvider clock) : ICommentCreatedHandler
{
    private sealed record Pending(CommentCreated Event, long Started);
    private readonly object gate = new();
    private readonly Dictionary<Guid, Pending> pending = [];
    private readonly HashSet<(Guid, Guid)> seen = [];
    private readonly Queue<(Guid, Guid)> seenOrder = new();
    private readonly Channel<CommentCreated> outgoing = Channel.CreateBounded<CommentCreated>(new BoundedChannelOptions(settings.Capacity)
    { FullMode = BoundedChannelFullMode.Wait, SingleReader = false, SingleWriter = false, AllowSynchronousContinuations = false });
    private bool connected;
    private bool completed;
    private long fallback, received, confirmed;
    public Guid OriginId { get; } = Guid.NewGuid();
    public ChannelReader<CommentCreated> Reader => outgoing.Reader;
    public (bool Connected, int Pending, long Fallback, long Received, long Confirmed) Statistics
    { get { lock (gate) return (connected, pending.Count, fallback, received, confirmed); } }

    public void Handle(CommentCreated notification)
    {
        lock (gate)
        {
            if (settings.Enabled && connected && pending.Count < settings.Capacity && !pending.ContainsKey(notification.EventId))
            {
                pending.Add(notification.EventId, new(notification, clock.GetTimestamp()));
                if (outgoing.Writer.TryWrite(notification)) return;
                pending.Remove(notification.EventId);
            }
            fallback++;
        }
        local.Handle(notification);
    }

    public bool ShouldPublish(Guid id) { lock (gate) return connected && pending.ContainsKey(id); }
    public void Confirmed() { lock (gate) confirmed++; }
    public void Receive(BrokerEnvelope envelope)
    {
        lock (gate)
        {
            if (envelope.OriginId == OriginId)
            {
                // A duplicate or late receipt must not resurrect an already delivered fallback.
                if (!pending.Remove(envelope.Event.EventId)) return;
            }
            else
            {
                var key = (envelope.OriginId, envelope.Event.EventId);
                if (!seen.Add(key)) return;
                seenOrder.Enqueue(key);
                while (seen.Count > settings.Capacity) seen.Remove(seenOrder.Dequeue());
            }
            received++;
        }
        if (envelope.OriginId != OriginId) cache.Handle(envelope.Event);
        local.Handle(envelope.Event);
    }

    public void SetConnected(bool value)
    {
        CommentCreated[] flush;
        lock (gate)
        {
            if (value && completed) return;
            connected = value;
            if (value) return;
            flush = pending.Values.Select(p => p.Event).ToArray(); pending.Clear();
            while (outgoing.Reader.TryRead(out _)) { }
            fallback += flush.Length;
        }
        foreach (var e in flush) local.Handle(e);
    }

    public void Expire()
    {
        CommentCreated[] expired;
        lock (gate)
        {
            expired = pending.Values.Where(p => clock.GetElapsedTime(p.Started) >= TimeSpan.FromMilliseconds(settings.FallbackDelayMilliseconds))
                .Select(p => p.Event).ToArray();
            foreach (var e in expired) pending.Remove(e.EventId);
            fallback += expired.Length;
        }
        foreach (var e in expired) local.Handle(e);
    }

    public void Complete() { lock (gate) completed = true; SetConnected(false); outgoing.Writer.TryComplete(); }
}
