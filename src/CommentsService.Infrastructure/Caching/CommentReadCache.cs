using CommentsService.Application.Events;

namespace CommentsService.Infrastructure.Caching;

// Singleton, process-local cache. No scoped persistence service is retained.
public sealed class CommentReadCache(CacheSettings settings, TimeProvider clock) : ICommentCreatedHandler
{
    private sealed record Entry(object Value, long Timestamp, long Size);
    private readonly Dictionary<object, Entry> entries = [];
    private readonly Queue<object> insertionOrder = new();
    private readonly object gate = new();
    private long generation;
    private long bytes;
    private long hits;
    private long misses;

    public (long Hits, long Misses, int Count, long Bytes) Statistics
    { get { lock (gate) return (hits, misses, entries.Count, bytes); } }

    public void Handle(CommentCreated notification)
    {
        lock (gate) { generation++; entries.Clear(); insertionOrder.Clear(); bytes = 0; }
    }

    public async Task<T> ReadAsync<T>(object key, Func<Task<T>> read, Func<T, long> size, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!settings.Enabled) return await read();
        long observedGeneration;
        lock (gate)
        {
            if (entries.TryGetValue(key, out var entry) &&
                clock.GetElapsedTime(entry.Timestamp) < TimeSpan.FromSeconds(settings.LifetimeSeconds))
            { hits++; return (T)entry.Value; }
            misses++;
            observedGeneration = generation;
        }
        // Failed/cancelled reads never populate the cache.
        var value = await read();
        token.ThrowIfCancellationRequested();
        var weight = size(value);
        lock (gate)
        {
            // Invalidation during a query must not let that query repopulate stale data.
            if (observedGeneration != generation || weight > settings.MaximumBytes || weight < 1) return value;
            // Retain an existing concurrent fill. Expired entries are removed with their FIFO key.
            if (entries.TryGetValue(key, out var existing) &&
                clock.GetElapsedTime(existing.Timestamp) < TimeSpan.FromSeconds(settings.LifetimeSeconds)) return value;
            if (entries.ContainsKey(key))
            {
                // Rebuild the small bounded FIFO to avoid duplicate keys growing without bound.
                bytes -= entries[key].Size; entries.Remove(key);
                var retained = insertionOrder.Where(item => !Equals(item, key)).ToArray();
                insertionOrder.Clear(); foreach (var item in retained) insertionOrder.Enqueue(item);
            }
            while (entries.Count >= settings.Capacity || bytes + weight > settings.MaximumBytes)
            {
                var oldest = insertionOrder.Dequeue();
                bytes -= entries[oldest].Size; entries.Remove(oldest);
            }
            entries.Add(key, new(value!, clock.GetTimestamp(), weight));
            insertionOrder.Enqueue(key); bytes += weight;
        }
        return value;
    }
}
