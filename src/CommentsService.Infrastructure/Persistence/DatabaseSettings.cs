namespace CommentsService.Infrastructure.Persistence;

public sealed class DatabaseSettings
{
    public string? Host { get; set; }
    public int Port { get; set; } = 5432;
    public string? Database { get; set; }
    public string? Username { get; set; }
    public int TimeoutSeconds { get; set; } = 15;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int MaximumPoolSize { get; set; } = 100;
}
