using CommentsService.Infrastructure.Search;
namespace CommentsService.Api.Search;
public sealed class SearchCapacity(SearchSettings settings) : IDisposable
{
    public SemaphoreSlim Slots { get; } = new(settings.MaximumConcurrentRequests);
    public void Dispose() => Slots.Dispose();
}
