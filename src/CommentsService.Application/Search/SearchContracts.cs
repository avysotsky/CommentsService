using CommentsService.Application.Comments;
using CommentsService.Application.Common;
namespace CommentsService.Application.Search;

public interface ICommentSearchSource
{
    Task<IReadOnlyList<SearchDocument>> ScanAsync(Guid? after, int limit, CancellationToken token);
    Task<IReadOnlyList<SearchDocument>> GetAsync(IReadOnlyList<Guid> ids, CancellationToken token);
    Task<IReadOnlyList<CommentDto>> HydrateAsync(IReadOnlyList<Guid> ids, CancellationToken token);
}
public interface ICommentSearchIndex
{
    Task<SearchIds> SearchAsync(string query, int page, int pageSize, CancellationToken token);
    Task IndexAsync(IReadOnlyList<SearchDocument> documents, CancellationToken token);
    Task<IReadOnlyList<Guid>> ScanIdsAsync(Guid? after, int limit, CancellationToken token);
    Task DeleteAsync(IReadOnlyList<Guid> ids, CancellationToken token);
}
public interface ISearchComments
{
    Task<Validated<SearchPage>> ExecuteAsync(string? query, int page, CancellationToken token);
}
public sealed class SearchUnavailableException : Exception
{
    public SearchUnavailableException() : base("Comment search is unavailable.") { }
}
