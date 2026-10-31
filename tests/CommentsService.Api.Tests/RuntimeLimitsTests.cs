using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommentsService.Api.Configuration;
using CommentsService.Application.Common;
using CommentsService.Infrastructure.Captcha;
using CommentsService.Infrastructure.TextFiles;
using CommentsService.Application.TextFiles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed class RuntimeLimitsTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture fixture;
    public RuntimeLimitsTests(ApiFixture fixture) => this.fixture = fixture;

    [Theory]
    [InlineData("Runtime:Limits:RootPageSize", "0")]
    [InlineData("Runtime:Limits:UserNameMaxLength", "101")]
    [InlineData("Runtime:Limits:TextFileMaxBytes", "102401")]
    [InlineData("Runtime:Limits:ImageMaxWidth", "321")]
    [InlineData("Runtime:Limits:ImageProcessingConcurrency", "0")]
    [InlineData("Runtime:Limits:MarkupMaxDepth", "33")]
    [InlineData("Runtime:Captcha:CodeLength", "3")]
    [InlineData("Runtime:Captcha:LifetimeSeconds", "0")]
    [InlineData("Runtime:Captcha:Alphabet", "AAAAAAAA")]
    [InlineData("Runtime:Requests:JsonMaxBytes", "0")]
    [InlineData("Runtime:Requests:BodyMaxBytes", "10")]
    public void InvalidPoliciesFailStartup(string key, string value)
    {
        var error = Assert.Throws<InvalidOperationException>(() => RuntimeConfiguration.Load(new ConfigurationManager(), [$"--{key}={value}"]));
        Assert.Equal("Runtime configuration could not be loaded or validated.", error.Message);
    }

    [Fact]
    public void CommandLineLoadsOperationalLimits()
    {
        var runtime = RuntimeConfiguration.Load(new ConfigurationManager(),
            ["--Runtime:Limits:RootPageSize=7", "--Runtime:Limits:TextMaxLength=12", "--Runtime:Captcha:CodeLength=8",
             "--Runtime:Captcha:LifetimeSeconds=60", "--Runtime:Requests:JsonMaxBytes=200"]);
        Assert.Equal(7, runtime.Limits.RootPageSize);
        Assert.Equal(12, runtime.Limits.TextMaxLength);
        Assert.Equal(8, runtime.Captcha.CodeLength);
        Assert.Equal(60, runtime.Captcha.LifetimeSeconds);
        Assert.Equal(200, runtime.Requests.JsonMaxBytes);
    }

    [Fact]
    public async Task IndependentHostUsesPolicyForPublicConfigPagingValidationAndRequests()
    {
        await using var configured = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            var runtime = (RuntimeSettings)services.Single(item => item.ServiceType == typeof(RuntimeSettings)).ImplementationInstance!;
            runtime.Limits.RootPageSize = 7;
            runtime.Limits.DefaultListLimit = 3;
            runtime.Limits.MaximumListLimit = 4;
            runtime.Limits.UserNameMaxLength = 4;
            runtime.Limits.TextMaxLength = 12;
            runtime.Captcha.CodeLength = 8;
            runtime.Requests.JsonMaxBytes = 200;
            runtime.Validate();
        }));
        using var client = configured.CreateClient();
        using var configResponse = await client.GetAsync("/api/config");
        Assert.Equal(HttpStatusCode.OK, configResponse.StatusCode);
        using var config = JsonDocument.Parse(await configResponse.Content.ReadAsStringAsync());
        var names = config.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray();
        Assert.Equal(new[] { "captchaAlphabet", "captchaCodeLength", "captchaLifetimeSeconds", "defaultListLimit", "emailMaxLength", "homePageMaxLength",
            "imageMaxHeight", "imageMaxWidth", "imageUploadMaxBytes", "maximumListLimit", "rootPageSize", "textFileMaxBytes",
            "textMaxLength", "userNameMaxLength", "realtimeEnabled", "reconnectDelayMilliseconds", "maximumReconnectDelayMilliseconds" }.Order(), names);
        Assert.Equal(8, config.RootElement.GetProperty("captchaCodeLength").GetInt32());
        using var page = JsonDocument.Parse(await client.GetStringAsync("/api/comments/page"));
        Assert.Equal(7, page.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/comments?limit=5")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/comments/preview", new { text = new string('a', 13) })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/comments/preview", new { text = new string('a', 12) })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/comments", new { userName = "LongName", email = "a@b.test", text = "Hi" })).StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, (await client.PostAsJsonAsync("/api/comments/preview", new { text = new string('a', 201) })).StatusCode);
        using var original = JsonDocument.Parse(await fixture.Client.GetStringAsync("/api/config"));
        Assert.Equal(25, original.RootElement.GetProperty("rootPageSize").GetInt32());
        Assert.Equal(6, original.RootElement.GetProperty("captchaCodeLength").GetInt32());
    }

    [Fact]
    public void CaptchaUsesLengthAlphabetCapacityExpiryAndSingleUse()
    {
        var clock = new Clock(); var renderer = new Renderer();
        var service = new MemoryCaptchaService(clock, renderer, new CaptchaSettings
            { Capacity = 1, LifetimeSeconds = 10, CodeLength = 8, Alphabet = "23456789" });
        var first = service.Issue();
        Assert.Equal(8, renderer.Code.Length);
        Assert.All(renderer.Code, value => Assert.Contains(value, "23456789"));
        Assert.Throws<CommentsService.Application.Captcha.CaptchaCapacityException>(() => service.Issue());
        Assert.True(service.Verify(first.Id, renderer.Code));
        Assert.False(service.Verify(first.Id, renderer.Code));
        var second = service.Issue(); clock.Now = clock.Now.AddSeconds(10);
        Assert.False(service.Verify(second.Id, renderer.Code));
    }

    [Fact]
    public async Task ReducedTextFileLimitCountsActualStreamBytes()
    {
        var processor = new Utf8TextFileProcessor(new ApplicationLimits { TextFileMaxBytes = 4 });
        Assert.True((await processor.ProcessAsync(new TextFileUpload("a.txt", 4, new MemoryStream("test"u8.ToArray())))).IsValid);
        Assert.False((await processor.ProcessAsync(new TextFileUpload("a.txt", 4, new MemoryStream("tests"u8.ToArray())))).IsValid);
    }

    [Fact]
    public void LongerCaptchaImageDoesNotClipCharacters()
    {
        using var bitmap = SkiaSharp.SKBitmap.Decode(new PngCaptchaRenderer().Render("23456789AB"));
        Assert.Equal(320, bitmap.Width);
        Assert.Equal(64, bitmap.Height);
    }

    [Fact]
    public async Task ReducedImageLimitsResizeProportionallyAndRejectOversizeStreams()
    {
        using var bitmap = new SkiaSharp.SKBitmap(40, 20);
        bitmap.Erase(SkiaSharp.SKColors.Blue);
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var png = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        var bytes = png.ToArray();
        var processor = new CommentsService.Infrastructure.Images.SkiaImageProcessor(new ApplicationLimits
            { ImageMaxWidth = 20, ImageMaxHeight = 20 });
        var result = await processor.ProcessAsync(new CommentsService.Application.Images.ImageUpload("a.png", bytes.Length, new MemoryStream(bytes)));
        Assert.True(result.IsValid);
        Assert.Equal(20, result.Value!.Width); Assert.Equal(10, result.Value.Height);
        processor = new(new ApplicationLimits { ImageUploadMaxBytes = bytes.Length - 1 });
        Assert.False((await processor.ProcessAsync(new("a.png", bytes.Length, new MemoryStream(bytes)))).IsValid);
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class Renderer : ICaptchaImageRenderer
    {
        public string Code { get; private set; } = "";
        public byte[] Render(string code) { Code = code; return [1]; }
    }
}
