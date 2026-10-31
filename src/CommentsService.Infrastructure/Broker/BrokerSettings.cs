namespace CommentsService.Infrastructure.Broker;

public sealed class BrokerSettings
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "broker";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "comments_app";
    public string Exchange { get; set; } = "commentsservice.comments.v1";
    public int Capacity { get; set; } = 128;
    public int OperationTimeoutSeconds { get; set; } = 2;
    public int ReconnectDelayMilliseconds { get; set; } = 2000;
    public int FallbackDelayMilliseconds { get; set; } = 1000;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host) || Host.Length > 253 || Host.Any(c => char.IsWhiteSpace(c) || char.IsControl(c) || c is '/' or '\\') ||
            string.IsNullOrWhiteSpace(Username) || Username.Length > 64 || Username.Any(char.IsControl) ||
            string.IsNullOrEmpty(Exchange) || Exchange.Length > 100 || Exchange.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '-' or '_')) ||
            Port is < 1 or > 65535 || Capacity is < 1 or > 1024 || OperationTimeoutSeconds is < 1 or > 10 ||
            ReconnectDelayMilliseconds is < 100 or > 30000 || FallbackDelayMilliseconds is < 100 or > 5000)
            throw new InvalidOperationException("Invalid broker settings.");
    }
}
