using CommentsService.Application.Search;
namespace CommentsService.Infrastructure.Search;
public sealed class SearchSettings
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = "http://elasticsearch:9200";
    public string Username { get; set; } = "comments_search";
    public string Index { get; set; } = "comments-v1";
    public int MaximumQueryLength { get; set; } = 200;
    public int PageSize { get; set; } = 10;
    public int MaximumPage { get; set; } = 100;
    public int TimeoutSeconds { get; set; } = 2;
    public int MaximumConcurrentRequests { get; set; } = 4;
    public int BatchSize { get; set; } = 50;
    public int BatchDelayMilliseconds { get; set; } = 1000;
    public int ReconcileIntervalSeconds { get; set; } = 30;
    public int FailureRetrySeconds { get; set; } = 5;
    public int Capacity { get; set; } = 256;
    public long MaximumIndexBytes { get; set; } = 1073741824;
    public SearchLimits Limits => new(MaximumQueryLength, PageSize, MaximumPage);
    public void Validate()
    {
        Limits.Validate();
        if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
            uri.UserInfo.Length != 0 || uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0 ||
            string.IsNullOrWhiteSpace(Username) || Username.Length > 64 || Username.Any(char.IsControl) ||
            Index is null || !Index.StartsWith("comments-", StringComparison.Ordinal) || Index.Length > 100 ||
            Index.Any(c => !char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c is not ('-' or '_')) ||
            TimeoutSeconds is < 1 or > 5 || MaximumConcurrentRequests is < 1 or > 8 || BatchSize is < 1 or > 100 ||
            BatchDelayMilliseconds is < 100 or > 60000 || ReconcileIntervalSeconds is < 5 or > 3600 || FailureRetrySeconds is < 1 or > 60 ||
            Capacity is < 1 or > 2048 || MaximumIndexBytes is < 1048576 or > 2147483648)
            throw new InvalidOperationException("Invalid search settings.");
    }
}
