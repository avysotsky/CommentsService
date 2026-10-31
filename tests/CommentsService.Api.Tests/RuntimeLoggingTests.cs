using CommentsService.Api.Configuration;
using CommentsService.Api.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed class RuntimeLoggingTests
{
    [Theory]
    [InlineData("Information")]
    [InlineData("Warning")]
    public async Task StructuredFileFiltersCategoriesRollsAndFlushesOnDispose(string minimumLevel)
    {
        var directory = Path.Combine(Path.GetTempPath(), "commentsservice-logs-" + Guid.NewGuid().ToString("N"));
        var builder = WebApplication.CreateBuilder();
        var settings = new RuntimeSettings { InstanceId = "test", Logging = new() { Directory = directory,
            MinimumLevel = minimumLevel, FileSizeLimitBytes = 1024, RetainedFileCountLimit = 2,
            Overrides = new() { ["CommentsService.Suppressed"] = "Warning" } } };
        RuntimeLogging.Configure(builder, settings);
        await using (var app = builder.Build())
        {
            var factory = app.Services.GetRequiredService<ILoggerFactory>();
            factory.CreateLogger("CommentsService.Suppressed").LogInformation("filtered-information");
            factory.CreateLogger("Microsoft.Secret").LogError("sensitive-provider-message");
            var logger = factory.CreateLogger("CommentsService.Probe");
            Parallel.For(0, 100, index => logger.LogInformation("Concurrent event {Sequence}", index));
            logger.LogWarning("final-warning");
        }
        var files = Directory.GetFiles(directory, "*.jsonl");
        Assert.InRange(files.Length, 1, 2);
        Assert.Equal(minimumLevel == "Information" ? 2 : 1, files.Length);
        var output = string.Join("", files.Select(File.ReadAllText));
        Assert.Contains("final-warning", output);
        Assert.Contains("\"InstanceId\":\"test\"", output);
        Assert.DoesNotContain("filtered-information", output);
        Assert.DoesNotContain("sensitive-provider-message", output);
        if (minimumLevel == "Warning") Assert.DoesNotContain("Concurrent event", output);
        foreach (var file in files) File.Delete(file);
        Directory.Delete(directory);
    }

    [Fact]
    public void FileAsLogDirectoryFailsAtStartup()
    {
        var path = Path.GetTempFileName();
        try
        {
            Assert.Throws<IOException>(() => RuntimeLogging.Configure(WebApplication.CreateBuilder(),
                new RuntimeSettings { Logging = new() { Directory = path } }));
        }
        finally { File.Delete(path); }
    }
}
