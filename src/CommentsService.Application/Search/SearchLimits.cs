namespace CommentsService.Application.Search;
public sealed record SearchLimits(int MaximumQueryLength = 200, int PageSize = 10, int MaximumPage = 100)
{
    public void Validate()
    {
        if (MaximumQueryLength is < 1 or > 500 || PageSize is < 1 or > 25 || MaximumPage is < 1 or > 100)
            throw new InvalidOperationException("Invalid search limits.");
    }
}
