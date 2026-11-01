namespace CommentsService.Infrastructure.Cloud;

public sealed class CloudSettings
{
    public bool Enabled { get; set; }
    // Explicit consent list. No scan/backfill and no implicit permission for future uploads.
    public Guid[] AllowedCommentIds { get; set; } = [];
    public Guid[] ReplayCommentIds { get; set; } = [];
    public int Capacity { get; set; } = 64;
    public int MaximumConcurrency { get; set; } = 2;
    public int MaximumAttempts { get; set; } = 3;
    public int WriteTimeoutSeconds { get; set; } = 5;
    public int ReadTimeoutMilliseconds { get; set; } = 1000;
    public int RetryDelayMilliseconds { get; set; } = 500;
    public int FailureThreshold { get; set; } = 5;
    public int BreakSeconds { get; set; } = 300;
    public long MaximumUploadBytes { get; set; } = 10 * 1024 * 1024;
    public long MaximumDownloadBytes { get; set; } = 20 * 1024 * 1024;
    public int MaximumRequests { get; set; } = 256;
    public void Validate()
    {
        if (AllowedCommentIds is null || ReplayCommentIds is null || AllowedCommentIds.Length > 64 ||
            ReplayCommentIds.Length > 64 || AllowedCommentIds.Contains(Guid.Empty) ||
            ReplayCommentIds.Any(id => !AllowedCommentIds.Contains(id)) || Capacity is < 1 or > 64 ||
            MaximumConcurrency is < 1 or > 2 || MaximumAttempts is < 1 or > 3 ||
            WriteTimeoutSeconds is < 1 or > 5 || ReadTimeoutMilliseconds is < 50 or > 1000 ||
            RetryDelayMilliseconds is < 1 or > 5000 || FailureThreshold is < 1 or > 5 ||
            BreakSeconds is < 1 or > 3600 || MaximumUploadBytes is < 1 or > 10485760 ||
            MaximumDownloadBytes is < 1 or > 20971520 || MaximumRequests is < 1 or > 256)
            throw new InvalidOperationException("Invalid cloud limits or consent list.");
    }
}
