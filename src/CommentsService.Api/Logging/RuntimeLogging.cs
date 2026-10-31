using CommentsService.Api.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace CommentsService.Api.Logging;

public static class RuntimeLogging
{
    public static void Configure(WebApplicationBuilder builder, RuntimeSettings settings)
    {
        var directory = Path.GetFullPath(settings.Logging.Directory);
        Directory.CreateDirectory(directory);
        var probe = Path.Combine(directory, ".write-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (File.Create(probe)) { }
            File.Delete(probe);
        }
        catch (IOException) { throw new InvalidOperationException("Log directory is not writable."); }
        catch (UnauthorizedAccessException) { throw new InvalidOperationException("Log directory is not writable."); }
        Serilog.Debugging.SelfLog.Enable(_ => Console.Error.WriteLine("Structured logging sink failure; check log storage."));
        builder.Logging.ClearProviders();
        builder.Services.AddSerilog((services, configuration) =>
        {
            configuration.MinimumLevel.Is(Enum.Parse<LogEventLevel>(settings.Logging.MinimumLevel))
                .Enrich.FromLogContext().Enrich.WithProperty("InstanceId", settings.InstanceId)
                .Filter.ByIncludingOnly(entry => entry.Properties.TryGetValue("SourceContext", out var source) &&
                    source is ScalarValue { Value: string category } && category.StartsWith("CommentsService.", StringComparison.Ordinal))
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.File(new JsonFormatter(), Path.Combine(directory, settings.InstanceId + "-.jsonl"),
                    rollingInterval: RollingInterval.Day, fileSizeLimitBytes: settings.Logging.FileSizeLimitBytes,
                    rollOnFileSizeLimit: true, retainedFileCountLimit: settings.Logging.RetainedFileCountLimit, shared: true);
            foreach (var pair in settings.Logging.Overrides)
                configuration.MinimumLevel.Override(pair.Key, Enum.Parse<LogEventLevel>(pair.Value));
        });
    }
}
