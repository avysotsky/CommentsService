using CommentsService.Application.Common;
namespace CommentsService.Application.Search;
public sealed class SearchComments(ICommentSearchIndex index, ICommentSearchSource source, SearchLimits limits) : ISearchComments
{
    public async Task<Validated<SearchPage>> ExecuteAsync(string? query, int page, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var text = query?.Trim(); var errors = new Dictionary<string,string[]>();
        if (string.IsNullOrEmpty(text) || text.Length > limits.MaximumQueryLength || text.Any(char.IsControl)) errors["query"] = ["Invalid search text."];
        if (page < 1 || page > limits.MaximumPage) errors["page"] = ["Invalid search page."];
        if (errors.Count != 0) return Validated<SearchPage>.Invalid(errors);
        var hits = await index.SearchAsync(text!, page, limits.PageSize, token);
        // SQL remains authoritative. Never expose index text or removed/stale documents.
        var rows = (await source.HydrateAsync(hits.Items, token)).ToDictionary(c => c.Id);
        return Validated<SearchPage>.Success(new(hits.Items.Where(rows.ContainsKey).Select(id => rows[id]).ToArray(), page,
            limits.PageSize, hits.HasMore && page < limits.MaximumPage));
    }
}
