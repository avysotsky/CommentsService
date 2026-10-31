using System.Text;
using CommentsService.Application.Events;
using CommentsService.Infrastructure.Broker;
using CommentsService.Infrastructure.Caching;
using CommentsService.Infrastructure.Notifications;
using CommentsService.Api.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed class MessageBrokerTests
{
    private sealed class Clock : TimeProvider
    {
        public long Now;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => Now;
    }
    private static CommentCreated Event() { var id = Guid.NewGuid(); return new(Guid.NewGuid(), id, null, id, DateTimeOffset.UtcNow); }
    private static (BrokerNotificationRouter Router, CommentNotificationQueue Local, CommentReadCache Cache, Clock Clock) Fixture(bool enabled = true, int capacity = 2)
    {
        var clock = new Clock(); var local = new CommentNotificationQueue(new() { Capacity = 16 });
        var cache = new CommentReadCache(new(), clock);
        return (new(new() { Enabled = enabled, Capacity = capacity, FallbackDelayMilliseconds = 100 }, local, cache, clock), local, cache, clock);
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public void DisabledOrDisconnectedUsesLocalPathWithoutAnyNetworkWork(bool enabled)
    {
        var f = Fixture(enabled); f.Router.Handle(Event());
        Assert.True(f.Local.Reader.TryRead(out _)); Assert.False(f.Router.Reader.TryRead(out _));
        Assert.Equal(0, f.Router.Statistics.Pending); Assert.Equal(1, f.Router.Statistics.Fallback);
    }

    [Fact]
    public void NormalBrokerReceiptSignalsOnceAndLateDuplicatesAreHarmless()
    {
        var f = Fixture(); f.Router.SetConnected(true); var e = Event(); f.Router.Handle(e);
        Assert.False(f.Local.Reader.TryRead(out _)); Assert.True(f.Router.Reader.TryRead(out var outgoing)); Assert.Equal(e, outgoing);
        f.Router.Confirmed(); f.Router.Receive(new(1, f.Router.OriginId, e)); f.Router.Receive(new(1, f.Router.OriginId, e));
        Assert.True(f.Local.Reader.TryRead(out _)); Assert.False(f.Local.Reader.TryRead(out _));
        Assert.Equal((true, 0, 0L, 1L, 1L), f.Router.Statistics);
    }

    [Fact]
    public void MissingConsumerReceiptExpiresEvenWhenConnectionReportsHealthy()
    {
        var f = Fixture(); f.Router.SetConnected(true); var e = Event(); f.Router.Handle(e);
        f.Clock.Now = 99; f.Router.Expire(); Assert.False(f.Local.Reader.TryRead(out _));
        f.Clock.Now = 100; f.Router.Expire(); Assert.True(f.Local.Reader.TryRead(out _));
        Assert.False(f.Router.ShouldPublish(e.EventId)); f.Router.Receive(new(1, f.Router.OriginId, e));
        Assert.False(f.Local.Reader.TryRead(out _)); Assert.Equal(0, f.Router.Statistics.Pending);
    }

    [Fact]
    public void OverflowDisconnectionAndReconnectStayBoundedAndRecoverWithoutRestart()
    {
        var f = Fixture(); f.Router.SetConnected(true);
        for (var i = 0; i < 100; i++) f.Router.Handle(Event());
        Assert.Equal(2, f.Router.Statistics.Pending); Assert.Equal(98, f.Router.Statistics.Fallback);
        f.Router.SetConnected(false); Assert.Equal(0, f.Router.Statistics.Pending); Assert.False(f.Router.Reader.TryRead(out _));
        Assert.Equal(100, f.Router.Statistics.Fallback);
        f.Router.SetConnected(true); var e = Event(); f.Router.Handle(e); Assert.True(f.Router.ShouldPublish(e.EventId));
        f.Router.Receive(new(1, f.Router.OriginId, e)); Assert.Equal(1, f.Router.Statistics.Received);
    }

    [Fact]
    public async Task ForeignEventInvalidatesCacheOnceAndDoesNotRepublish()
    {
        var f = Fixture(); var reads = 0;
        Task<string> Read() { reads++; return Task.FromResult("value"); }
        await f.Cache.ReadAsync("key", Read, _ => 1, default);
        var envelope = new BrokerEnvelope(1, Guid.NewGuid(), Event()); f.Router.Receive(envelope);
        await f.Cache.ReadAsync("key", Read, _ => 1, default);
        f.Router.Receive(envelope); await f.Cache.ReadAsync("key", Read, _ => 1, default);
        Assert.Equal(2, reads); Assert.True(f.Local.Reader.TryRead(out _)); Assert.False(f.Local.Reader.TryRead(out _));
        Assert.False(f.Router.Reader.TryRead(out _)); Assert.Equal(1, f.Router.Statistics.Received);
    }

    [Fact]
    public void ConcurrentExpiryReceiptAndDisconnectCannotLeakPendingHints()
    {
        var f = Fixture(capacity: 16); f.Router.SetConnected(true);
        Parallel.For(0, 1000, i => {
            var e = Event(); f.Router.Handle(e);
            if (i % 2 == 0) f.Router.Receive(new(1, f.Router.OriginId, e));
            if (i % 7 == 0) f.Router.Expire();
            if (i % 13 == 0) { f.Router.SetConnected(false); f.Router.SetConnected(true); }
        });
        f.Router.Complete(); f.Router.SetConnected(true); Assert.False(f.Router.Statistics.Connected); Assert.Equal(0, f.Router.Statistics.Pending);
        Assert.False(f.Router.Reader.TryRead(out _)); Assert.True(f.Local.Reader.TryRead(out _));
    }

    [Fact]
    public void EnvelopeIsVersionedBoundedAndContainsNoContentOrSecrets()
    {
        var envelope = new BrokerEnvelope(1, Guid.NewGuid(), Event());
        var bytes = BrokerEnvelopeCodec.Encode(envelope); var json = Encoding.UTF8.GetString(bytes);
        Assert.Equal(envelope, BrokerEnvelopeCodec.Decode(bytes)); Assert.True(bytes.Length < 1024);
        foreach (var key in new[] { "userName", "email", "password", "ipAddress", "userAgent", "text", "image", "textFile" }) Assert.DoesNotContain(key, json);
        Assert.Null(BrokerEnvelopeCodec.Decode(new byte[1025]));
        Assert.Null(BrokerEnvelopeCodec.Decode(new byte[] { 0xff }));
        foreach (var invalid in new[] { json.Replace("\"version\":1", "\"version\":2"),
            json.Replace("\"version\":1", "\"version\":1,\"version\":1"),
            json.Replace("\"version\":1", "\"version\":1,\"private\":true"),
            json.Replace("\"version\":1", "\"Version\":1"), "{}", "null", "[]" })
            Assert.Null(BrokerEnvelopeCodec.Decode(Encoding.UTF8.GetBytes(invalid)));
    }

    [Theory]
    [InlineData("Capacity", "0")] [InlineData("Capacity", "1025")]
    [InlineData("Port", "65536")] [InlineData("OperationTimeoutSeconds", "0")]
    [InlineData("ReconnectDelayMilliseconds", "0")] [InlineData("FallbackDelayMilliseconds", "0")]
    [InlineData("Host", "https://private")] [InlineData("Exchange", "bad/path")]
    public void BrokerPoliciesFailStartupWithoutEchoingValues(string key, string value)
    {
        var error = Assert.Throws<InvalidOperationException>(() => CommentsService.Api.Configuration.RuntimeConfiguration.Load(new Microsoft.Extensions.Configuration.ConfigurationManager(), [$"--Runtime:Broker:{key}={value}"]));
        Assert.Equal("Runtime configuration could not be loaded or validated.", error.Message);
    }

    [Fact]
    public async Task DisconnectedWorkerStopsPromptlyAndLeavesLocalQueueWorking()
    {
        var f = Fixture(); var settings = new BrokerSettings { Enabled = true, Host = "127.0.0.1", Port = 1, ReconnectDelayMilliseconds = 100 };
        var transport = new RabbitCommentBroker(settings, f.Router, NullLogger<RabbitCommentBroker>.Instance);
        using var worker = new MessageBrokerWorker(settings, f.Router, transport, NullLogger<MessageBrokerWorker>.Instance);
        await worker.StartAsync(default); f.Router.Handle(Event());
        Assert.True(f.Local.Reader.TryRead(out _));
        await worker.StopAsync(default).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(0, f.Router.Statistics.Pending); Assert.False(f.Router.Statistics.Connected);
    }
}
