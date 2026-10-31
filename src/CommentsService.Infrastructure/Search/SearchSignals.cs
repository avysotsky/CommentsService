using System.Threading.Channels;
namespace CommentsService.Infrastructure.Search;
public sealed class SearchSignals(SearchSettings settings)
{
    private readonly Channel<Guid> ids = Channel.CreateBounded<Guid>(new BoundedChannelOptions(settings.Capacity)
    { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true, SingleWriter = false });
    public void Notify(Guid id) => ids.Writer.TryWrite(id);
    public Guid[] Drain(int limit)
    {
        var result = new HashSet<Guid>();
        for (var i=0; i<limit && ids.Reader.TryRead(out var id); i++) result.Add(id);
        return result.ToArray();
    }
}
