using CommentsService.Infrastructure.Persistence;

namespace CommentsService.Api.Configuration;

public sealed class RuntimeSettings
{
    public string InstanceId { get; set; } = "commentsservice";
    public int HealthPort { get; set; } = 8080;
    public DatabaseSettings Database { get; set; } = new();
    public LogSettings Logging { get; set; } = new();
    public CommentsService.Application.Common.ApplicationLimits Limits { get; set; } = new();
    public CommentsService.Infrastructure.Captcha.CaptchaSettings Captcha { get; set; } = new();
    public RequestSettings Requests { get; set; } = new();
    public ProxySettings Proxy { get; set; } = new();
    public CommentsService.Infrastructure.Caching.CacheSettings Cache { get; set; } = new();
    public CommentsService.Infrastructure.Notifications.NotificationSettings Notifications { get; set; } = new();
    public CommentsService.Api.Realtime.RealtimeSettings Realtime { get; set; } = new();

    public GraphQLSettings GraphQL { get; set; } = new();

    public CommentsService.Infrastructure.Broker.BrokerSettings Broker { get; set; } = new();

    public void Validate()
    {
        if (Broker is null) throw new InvalidOperationException("Missing broker settings.");
        Broker.Validate();
        if (GraphQL is null) throw new InvalidOperationException("Missing GraphQL settings.");
        GraphQL.Validate();
        if (Cache is null) throw new InvalidOperationException("Missing read cache settings.");
        Cache.Validate();
        if (Notifications is null) throw new InvalidOperationException("Missing notification settings.");
        Notifications.Validate();
        if (Realtime is null) throw new InvalidOperationException("Missing realtime settings.");
        Realtime.Validate();
        if (Limits is null || Captcha is null || Requests is null) throw new InvalidOperationException("Missing runtime policies.");
        Limits.Validate(); Captcha.Validate(); Requests.Validate();
        if (Logging is null) throw new InvalidOperationException("Missing logging settings.");
        Logging.Validate();
        if (Proxy is null) throw new InvalidOperationException("Missing proxy settings.");
        Proxy.Validate();
        if (InstanceId is null || Database is null || InstanceId.Length is < 1 or > 64 || InstanceId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_')) ||
            HealthPort is < 1 or > 65535 || Database.Port is < 1 or > 65535 ||
            Database.TimeoutSeconds is < 1 or > 300 || Database.CommandTimeoutSeconds is < 1 or > 300 ||
            Database.MaximumPoolSize is < 1 or > 1000)
            throw new InvalidOperationException("Invalid runtime configuration. Check instance, port and database limits.");
    }
}
