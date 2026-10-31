using Serilog.Events;

namespace CommentsService.Api.Configuration;

public sealed class LogSettings
{
    public string MinimumLevel { get; set; } = "Information";
    public Dictionary<string, string> Overrides { get; set; } = new();
    public string Directory { get; set; } = "logs";
    public long FileSizeLimitBytes { get; set; } = 10485760;
    public int RetainedFileCountLimit { get; set; } = 14;

    public void Validate()
    {
        if (!Enum.TryParse<LogEventLevel>(MinimumLevel, false, out var level) || !Enum.IsDefined(level) ||
            string.IsNullOrWhiteSpace(Directory) || FileSizeLimitBytes is < 1024 or > 1073741824 ||
            RetainedFileCountLimit is < 1 or > 365 || Overrides is null ||
            Overrides.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || !Enum.TryParse<LogEventLevel>(pair.Value, false, out var value) || !Enum.IsDefined(value)))
            throw new InvalidOperationException("Invalid logging settings.");
    }
}
