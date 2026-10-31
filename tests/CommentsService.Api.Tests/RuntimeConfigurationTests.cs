using CommentsService.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed class RuntimeConfigurationTests
{
    [Fact]
    public void DefaultsAndCommandLineAreValidated()
    {
        var configuration = new ConfigurationManager();
        var settings = RuntimeConfiguration.Load(configuration, ["--Runtime:InstanceId=instance-two", "--Runtime:Database:MaximumPoolSize=12",
            "--Runtime:Database:Host=cli-host", "--Runtime:Database:Port=5439", "--Runtime:Cache:Enabled=false"]);
        Assert.Equal("instance-two", settings.InstanceId);
        Assert.Equal(12, settings.Database.MaximumPoolSize);
        Assert.Equal(8080, settings.HealthPort);
        Assert.Equal("cli-host", settings.Database.Host);
        Assert.Equal(5439, settings.Database.Port);
        Assert.False(settings.Cache.Enabled);
        var old = System.Text.Json.JsonSerializer.Deserialize<RuntimeSettings>("{\"InstanceId\":\"old-config\"}")!;
        old.Validate();
        Assert.True(old.Cache.Enabled);
        Assert.Equal(15, old.Cache.LifetimeSeconds);
    }

    [Theory]
    [InlineData("Runtime:Database:Port", "0")]
    [InlineData("Runtime:Database:MaximumPoolSize", "1001")]
    [InlineData("Runtime:Database:CommandTimeoutSeconds", "0")]
    [InlineData("Runtime:Database:TimeoutSeconds", "301")]
    [InlineData("Runtime:InstanceId", "../unsafe")]
    [InlineData("Runtime:HealthPort", "65536")]
    [InlineData("Runtime:Logging:MinimumLevel", "bogus")]
    [InlineData("Runtime:Logging:FileSizeLimitBytes", "0")]
    [InlineData("Runtime:Logging:RetainedFileCountLimit", "0")]
    public void InvalidConfigurationFailsWithoutEchoingValues(string key, string value)
    {
        var configuration = new ConfigurationManager();
        var error = Assert.Throws<InvalidOperationException>(() => RuntimeConfiguration.Load(configuration, [$"--{key}={value}"]));
        Assert.Equal("Runtime configuration could not be loaded or validated.", error.Message);
        Assert.Null(error.InnerException);
    }
}
