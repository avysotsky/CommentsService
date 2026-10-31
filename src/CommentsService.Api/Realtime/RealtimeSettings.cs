namespace CommentsService.Api.Realtime;

public sealed class RealtimeSettings
{
    public bool Enabled { get; set; } = true;
    public int MaximumConnections { get; set; } = 256;
    public int SendTimeoutSeconds { get; set; } = 5;
    public int KeepAliveSeconds { get; set; } = 20;
    public int KeepAliveTimeoutSeconds { get; set; } = 10;
    public int ReconnectDelayMilliseconds { get; set; } = 1000;
    public int MaximumReconnectDelayMilliseconds { get; set; } = 30000;
    public void Validate()
    {
        if (MaximumConnections is < 1 or > 4096 || SendTimeoutSeconds is < 1 or > 30 ||
            KeepAliveSeconds is < 5 or > 60 || KeepAliveTimeoutSeconds is < 1 or > 30 ||
            ReconnectDelayMilliseconds is < 100 or > 60000 ||
            MaximumReconnectDelayMilliseconds < ReconnectDelayMilliseconds || MaximumReconnectDelayMilliseconds > 120000)
            throw new InvalidOperationException("Invalid realtime settings.");
    }
}
