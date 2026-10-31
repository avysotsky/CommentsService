using CommentsService.Infrastructure.Persistence;

namespace CommentsService.Api.Configuration;

public sealed class RuntimeSettings
{
    public string InstanceId { get; set; } = "commentsservice";
    public int HealthPort { get; set; } = 8080;
    public DatabaseSettings Database { get; set; } = new();

    public void Validate()
    {
        if (InstanceId is null || Database is null || InstanceId.Length is < 1 or > 64 || InstanceId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')) ||
            HealthPort is < 1 or > 65535 || Database.Port is < 1 or > 65535 ||
            Database.TimeoutSeconds is < 1 or > 300 || Database.CommandTimeoutSeconds is < 1 or > 300 ||
            Database.MaximumPoolSize is < 1 or > 1000)
            throw new InvalidOperationException("Invalid runtime configuration. Check instance, port and database limits.");
    }
}
