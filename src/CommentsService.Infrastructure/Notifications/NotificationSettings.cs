namespace CommentsService.Infrastructure.Notifications;

public sealed class NotificationSettings
{
    public int Capacity { get; set; } = 64;
    public int MaximumAttempts { get; set; } = 3;
    public int RetryDelayMilliseconds { get; set; } = 200;
    public void Validate()
    {
        if (Capacity is < 1 or > 4096 || MaximumAttempts is < 1 or > 10 || RetryDelayMilliseconds is < 1 or > 10000)
            throw new InvalidOperationException("Invalid notification queue settings.");
    }
}
