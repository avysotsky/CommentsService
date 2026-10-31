using System.Net;
using System.Net.WebSockets;
using System.Text;
using CommentsService.Api.Realtime;
using CommentsService.Api.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed partial class CommentsHttpTests
{
    [Fact]
    public async Task CommittedPostReachesTwoWebSocketsAndDisconnectedClientsCanReconnect()
    {
        var client = fixture.Factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Origin = "http://localhost";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var uri = new Uri("ws://localhost/api/comments/live");
        using var first = await client.ConnectAsync(uri, timeout.Token);
        using var second = await client.ConnectAsync(uri, timeout.Token);
        Assert.Equal("{\"type\":\"connected\"}", await ReceiveLive(first, timeout.Token));
        Assert.Equal("{\"type\":\"connected\"}", await ReceiveLive(second, timeout.Token));
        using var post = await fixture.PostAsync(ApiFixture.Payload("Live event integration"));
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        Assert.Equal("{\"type\":\"commentsChanged\"}", await ReceiveLive(first, timeout.Token));
        Assert.Equal("{\"type\":\"commentsChanged\"}", await ReceiveLive(second, timeout.Token));
        await first.CloseAsync(WebSocketCloseStatus.NormalClosure, "", timeout.Token);
        using var reconnected = await client.ConnectAsync(uri, timeout.Token);
        Assert.Equal("{\"type\":\"connected\"}", await ReceiveLive(reconnected, timeout.Token));
        await second.CloseAsync(WebSocketCloseStatus.NormalClosure, "", timeout.Token);
        await reconnected.CloseAsync(WebSocketCloseStatus.NormalClosure, "", timeout.Token);
    }

    [Fact]
    public async Task WebSocketRejectsForeignOrMissingOriginAndPlainHttp()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await fixture.Client.GetAsync("/api/comments/live")).StatusCode);
        foreach (var origin in new[] { "https://foreign.test", "null", "http://localhost/other", "" })
        {
            var client = fixture.Factory.Server.CreateWebSocketClient();
            client.ConfigureRequest = request => { if (origin.Length > 0) request.Headers.Origin = origin; };
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConnectAsync(new Uri("ws://localhost/api/comments/live"), default));
            Assert.Contains("403", error.Message);
        }
    }

    [Fact]
    public async Task DisabledRealtimeReturnsNotFound()
    {
        await using var configured = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            ((RuntimeSettings)services.Single(item => item.ServiceType == typeof(RuntimeSettings)).ImplementationInstance!).Realtime.Enabled = false));
        using var client = configured.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/comments/live")).StatusCode);
    }

    [Fact]
    public async Task WebSocketConnectionCapacityReturns503()
    {
        await using var configured = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            ((RuntimeSettings)services.Single(item => item.ServiceType == typeof(RuntimeSettings)).ImplementationInstance!).Realtime.MaximumConnections = 1));
        using var http = configured.CreateClient();
        var client = configured.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Origin = "http://localhost";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var first = await client.ConnectAsync(new Uri("ws://localhost/api/comments/live"), timeout.Token);
        Assert.Equal("{\"type\":\"connected\"}", await ReceiveLive(first, timeout.Token));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => client.ConnectAsync(new Uri("ws://localhost/api/comments/live"), timeout.Token));
        Assert.Contains("503", error.Message);
        await first.CloseAsync(WebSocketCloseStatus.NormalClosure, "", timeout.Token);
    }

    private static async Task<string> ReceiveLive(WebSocket socket, CancellationToken token)
    {
        var data = new byte[256];
        var frame = await socket.ReceiveAsync(data.AsMemory(), token);
        Assert.Equal(WebSocketMessageType.Text, frame.MessageType); Assert.True(frame.EndOfMessage);
        return Encoding.UTF8.GetString(data, 0, frame.Count);
    }
}

public sealed class RealtimeUnitTests
{
    [Fact]
    public async Task SlowClientHasOnePendingHintAndDisposalReleasesConnectionSlot()
    {
        var broadcaster = new LiveCommentBroadcaster(new() { MaximumConnections = 1 });
        using var first = broadcaster.TrySubscribe(); Assert.NotNull(first);
        Assert.Null(broadcaster.TrySubscribe());
        for (var i = 0; i < 1000; i++) await broadcaster.DeliverAsync(default);
        Assert.True(first.Reader.TryRead(out _)); Assert.False(first.Reader.TryRead(out _));
        first.Dispose();
        using var next = broadcaster.TrySubscribe(); Assert.NotNull(next);
    }

    [Theory]
    [InlineData(0, 5, 1000, 30000)] [InlineData(4097, 5, 1000, 30000)]
    [InlineData(256, 0, 1000, 30000)] [InlineData(256, 5, 99, 30000)] [InlineData(256, 5, 1000, 999)]
    public void RealtimePoliciesAreValidated(int maximum, int timeout, int delay, int maximumDelay) =>
        Assert.Throws<InvalidOperationException>(() => new RealtimeSettings { MaximumConnections = maximum,
            SendTimeoutSeconds = timeout, ReconnectDelayMilliseconds = delay, MaximumReconnectDelayMilliseconds = maximumDelay }.Validate());
}
