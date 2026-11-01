using System.Net;
using System.Text;
using CommentsService.Application.Cloud;
using CommentsService.Application.Events;
using CommentsService.Infrastructure.Cloud;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
namespace CommentsService.Api.Tests;

public sealed class CloudAttachmentTests
{
    private static CloudAttachment File(AttachmentKind kind = AttachmentKind.Text) => new(Guid.NewGuid(), Guid.NewGuid(), kind, Encoding.UTF8.GetBytes("small own test\r\nПривіт"));
    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Interlocked.Increment(ref Calls); return send(request, token); }
    }
    private static HttpResponseMessage Bucket(bool publicBucket = false) => new(HttpStatusCode.OK)
    { Content = new StringContent("{\"id\":\"comments-attachments\",\"public\":" + (publicBucket ? "true" : "false") + ",\"allowed_mime_types\":[\"image/png\",\"text/plain\"],\"file_size_limit\":5242880}") };
    private static SupabaseAttachmentCloud Cloud(CloudAttachment a, Handler handler, CloudSettings? settings = null, string? url = "https://test.supabase.co", string? secret = "sb_secret_test", string database = "comments_test") =>
        new(settings ?? new() { Enabled = true, AllowedCommentIds = [a.CommentId], RetryDelayMilliseconds = 1 }, new(handler), NullLogger<SupabaseAttachmentCloud>.Instance, database, url, secret, "comments-attachments");
    [Theory]
    [InlineData(null, "sb_secret_test")] [InlineData("http://test.supabase.co", "sb_secret_test")]
    [InlineData("https://localhost", "sb_secret_test")] [InlineData("https://test.supabase.co@evil.test", "sb_secret_test")]
    [InlineData("https://test.supabase.co", null)] [InlineData("https://test.supabase.co", "eyJlegacy")]
    public async Task MissingOrInvalidCredentialsHaveNoNetworkDependency(string? url, string? secret)
    {
        var a = File(); var h = new Handler((_,_) => throw new Exception("must not connect"));
        using var cloud = Cloud(a,h,url:url,secret:secret);
        Assert.Same(a.Data, await cloud.ReadOrOriginalAsync(a)); Assert.False(await cloud.CopyAsync(a)); Assert.Equal(0,h.Calls);
    }
    [Fact]
    public async Task DefaultDisabledAndEmptyConsentNeverContactCloud()
    {
        var a=File();var h=new Handler((_,_)=>throw new Exception());
        using var cloud=Cloud(a,h,new());
        Assert.False(await cloud.CopyAsync(a));Assert.Same(a.Data,await cloud.ReadOrOriginalAsync(a));Assert.Equal(0,h.Calls);
        using var enabled=Cloud(a,h,new(){Enabled=true});Assert.False(await enabled.CopyAsync(a));Assert.Equal(0,h.Calls);
    }
    [Theory] [InlineData(AttachmentKind.Image)] [InlineData(AttachmentKind.Text)]
    public async Task AuthorizedCopyAndReadPreserveExactBytesAndKeepSecretOutOfUrl(AttachmentKind kind)
    {
        var a=File(kind);var gets=0;string? key=null;
        var h=new Handler(async (r,t)=>{
            Assert.Equal("sb_secret_test",r.Headers.GetValues("apikey").Single());
            Assert.DoesNotContain("sb_secret",r.RequestUri!.ToString());
            if(r.RequestUri.AbsolutePath.Contains("/bucket/"))return Bucket();
            key=r.RequestUri.AbsolutePath;
            if(r.Method==HttpMethod.Post){Assert.Equal(a.Data,await r.Content!.ReadAsByteArrayAsync(t));Assert.Equal("true",r.Headers.GetValues("x-upsert").Single());return new(HttpStatusCode.OK){Content=new StringContent("{}")};}
            gets++;return new(HttpStatusCode.OK){Content=new ByteArrayContent(a.Data)};
        });
        using var cloud=Cloud(a,h);Assert.True(await cloud.CopyAsync(a));Assert.Equal(a.Data,await cloud.ReadOrOriginalAsync(a));
        Assert.Equal(1,gets);Assert.Contains(a.Id.ToString("N"),key);Assert.Equal(3,h.Calls);
    }
    [Theory] [InlineData(400)] [InlineData(401)] [InlineData(402)] [InlineData(403)]
    public async Task CredentialsAndQuotaRestrictionsLatchUntilOperatorRestart(int code)
    {
        var a=File();var h=new Handler((r,_)=>Task.FromResult(r.RequestUri!.AbsolutePath.Contains("/bucket/")?Bucket():new HttpResponseMessage((HttpStatusCode)code)));
        using var cloud=Cloud(a,h);Assert.False(await cloud.CopyAsync(a));var count=h.Calls;
        Assert.False(await cloud.CopyAsync(a));Assert.Same(a.Data,await cloud.ReadOrOriginalAsync(a));Assert.Equal(count,h.Calls);Assert.True(cloud.Statistics.Stopped);
    }
    [Fact]
    public async Task PublicBucketFailsClosedBeforeAnyObjectOperation()
    {
        var a=File();var h=new Handler((_,_)=>Task.FromResult(Bucket(true)));using var cloud=Cloud(a,h);
        Assert.False(await cloud.CopyAsync(a));Assert.Same(a.Data,await cloud.ReadOrOriginalAsync(a));Assert.Equal(1,h.Calls);
    }
    [Theory] [InlineData("corrupt")] [InlineData("")] [InlineData("oversized data that exceeds the original size by a large margin")]
    public async Task CorruptTruncatedAndOversizedCloudBytesFallBackBeforeResponse(string text)
    {
        var a=File();var h=new Handler((r,_)=>Task.FromResult(r.RequestUri!.AbsolutePath.Contains("/bucket/")?Bucket():new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(text)}));
        using var cloud=Cloud(a,h);Assert.Same(a.Data,await cloud.ReadOrOriginalAsync(a));
    }
    [Theory] [InlineData(302)] [InlineData(404)] [InlineData(429)] [InlineData(503)]
    public async Task ReadFailuresKeepCanonicalBytesWithOneAttempt(int code)
    {
        var a=File();var h=new Handler((r,_)=>Task.FromResult(r.RequestUri!.AbsolutePath.Contains("/bucket/")?Bucket():new HttpResponseMessage((HttpStatusCode)code)));
        using var cloud=Cloud(a,h);Assert.Same(a.Data,await cloud.ReadOrOriginalAsync(a));Assert.Equal(2,h.Calls);
    }
    [Fact]
    public async Task OpenConnectionTimeoutIsBoundedAndReturnsOriginal()
    {
        var a=File();var h=new Handler(async (_,t)=>{await Task.Delay(Timeout.Infinite,t);return Bucket();});
        using var cloud=Cloud(a,h,new(){Enabled=true,AllowedCommentIds=[a.CommentId],ReadTimeoutMilliseconds=50});
        var watch=System.Diagnostics.Stopwatch.StartNew();Assert.Same(a.Data,await cloud.ReadOrOriginalAsync(a));Assert.True(watch.Elapsed<TimeSpan.FromSeconds(2));
    }
    [Fact]
    public async Task TransientWriteRetriesAreFiniteAndCountEveryAttempt()
    {
        var a=File();var posts=0;var h=new Handler((r,_)=>{
            if(r.RequestUri!.AbsolutePath.Contains("/bucket/"))return Task.FromResult(Bucket());
            posts++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));});
        using var cloud=Cloud(a,h);Assert.False(await cloud.CopyAsync(a));Assert.Equal(3,posts);Assert.Equal(3*a.Data.Length,cloud.Statistics.UploadBytes);
    }
    [Fact]
    public async Task RequestAndByteBudgetsStopWorkWithoutAQueueLoop()
    {
        var a=File();var h=new Handler((_,_)=>Task.FromResult(Bucket()));
        using var cloud=Cloud(a,h,new(){Enabled=true,AllowedCommentIds=[a.CommentId],MaximumDownloadBytes=1});
        Assert.False(await cloud.CopyAsync(a));Assert.Same(a.Data,await cloud.ReadOrOriginalAsync(a));Assert.Equal(0,h.Calls);
        using var small=Cloud(a,h,new(){Enabled=true,AllowedCommentIds=[a.CommentId],MaximumRequests=1});
        Assert.False(await small.CopyAsync(a));Assert.Equal(1,small.Statistics.Requests);
    }
    [Fact]
    public void KeysPartitionDatabasesAndTypesAndNeverUseUserFilenames()
    {
        var a=File();var h=new Handler((_,_)=>throw new Exception());using var x=Cloud(a,h);using var y=Cloud(a,new Handler((_,_)=>throw new Exception()),database:"comments");
        Assert.NotEqual(x.ObjectKey(a),y.ObjectKey(a));Assert.NotEqual(x.ObjectKey(a),x.ObjectKey(a with {Kind=AttachmentKind.Image}));
        Assert.NotEqual(x.ObjectKey(a),x.ObjectKey(a with {Data=[1]}));Assert.StartsWith("v1/",x.ObjectKey(a));
    }
    [Fact]
    public void SignalConsentDeduplicationCapacityAndExplicitReplayAreBounded()
    {
        var a=File();var b=Guid.NewGuid();var settings=new CloudSettings(){Enabled=true,AllowedCommentIds=[a.CommentId,b],ReplayCommentIds=[b],Capacity=1};
        var signals=new CloudCopySignals(settings);
        CommentCreated E(Guid id)=>new(Guid.NewGuid(),id,null,id,DateTimeOffset.UtcNow);
        signals.Handle(E(Guid.NewGuid()));signals.Handle(E(a.CommentId));signals.Handle(E(a.CommentId));signals.Handle(E(b));
        Assert.True(signals.TryTake(out var id));Assert.Equal(a.CommentId,id);Assert.False(signals.TryTake(out _));
        signals.Complete(id);signals.Handle(E(b));Assert.True(signals.TryTake(out id));Assert.Equal(b,id);Assert.Equal(new[]{b},signals.ReplayIds);
    }
    [Fact]
    public void ReplayCannotAuthorizeAnUnlistedCommentOrUnboundedPolicy()
    {
        Assert.Throws<InvalidOperationException>(()=>new CloudSettings(){ReplayCommentIds=[Guid.NewGuid()]}.Validate());
        Assert.Throws<InvalidOperationException>(()=>new CloudSettings(){MaximumAttempts=4}.Validate());
        Assert.Throws<InvalidOperationException>(()=>new CloudSettings(){MaximumConcurrency=3}.Validate());
        new CloudSettings().Validate();
    }
    [Fact]
    public async Task SaturatedCloudSlotsReturnSqlImmediatelyAndDoNotQueueReaders()
    {
        var a=File();var entered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active=0;var peak=0;
        var h=new Handler(async(r,t)=>{
            var n=Interlocked.Increment(ref active);peak=Math.Max(peak,n);
            if(n==2)entered.TrySetResult();
            try{await release.Task.WaitAsync(t);return Bucket();}finally{Interlocked.Decrement(ref active);}
        });
        using var cloud=Cloud(a,h);
        var first=cloud.ReadOrOriginalAsync(a);var second=cloud.ReadOrOriginalAsync(a);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var watch=System.Diagnostics.Stopwatch.StartNew();Assert.Same(a.Data,await cloud.ReadOrOriginalAsync(a));Assert.True(watch.ElapsedMilliseconds<200);
        release.TrySetResult();await Task.WhenAll(first,second);Assert.Equal(2,peak);
    }
    [Theory] [InlineData("not json")] [InlineData("{}")]
    public async Task MalformedMetadataNeverCausesAnObjectUpload(string metadata)
    {
        var a=File();var h=new Handler((_,_)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(metadata)}));
        using var cloud=Cloud(a,h);Assert.False(await cloud.CopyAsync(a));Assert.Same(a.Data,await cloud.ReadOrOriginalAsync(a));
    }
    private sealed class Source(CloudAttachment attachment) : ICloudAttachmentSource
    {
        public Task<CloudAttachment?> FindByCommentAsync(Guid id,CancellationToken token=default) => Task.FromResult<CloudAttachment?>(id==attachment.CommentId?attachment:null);
    }
    private sealed class BlockedMirror : IAttachmentCloud
    {
        public readonly TaskCompletionSource<CloudAttachment> Called=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<byte[]> ReadOrOriginalAsync(CloudAttachment a,CancellationToken token=default)=>Task.FromResult(a.Data);
        public async Task<bool> CopyAsync(CloudAttachment a,CancellationToken token=default)
        { Called.TrySetResult(a);await Release.Task.WaitAsync(token);return false; }
    }
    [Fact]
    public async Task PostCommitEventDispatchDoesNotWaitForSlowCloudWorker()
    {
        var a=File();var signals=new CloudCopySignals(new(){Enabled=true,AllowedCommentIds=[a.CommentId]});var mirror=new BlockedMirror();
        var services=new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddScoped<ICloudAttachmentSource>(services,_=>new Source(a));
        Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<IAttachmentCloud>(services,mirror);
        await using var provider=Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);
        using var worker=new CommentsService.Api.Cloud.CloudAttachmentWorker(
            Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>(provider),signals,NullLogger<CommentsService.Api.Cloud.CloudAttachmentWorker>.Instance);
        await worker.StartAsync(default);
        var dispatcher=new CommentsService.Infrastructure.Events.CommentEvents([signals],NullLogger<CommentsService.Infrastructure.Events.CommentEvents>.Instance);
        dispatcher.Publish(new(Guid.NewGuid(),a.CommentId,null,a.CommentId,DateTimeOffset.UtcNow));
        var copied=await mirror.Called.Task.WaitAsync(TimeSpan.FromSeconds(3));Assert.Equal(a.Data,copied.Data);
        Assert.False(mirror.Release.Task.IsCompleted);mirror.Release.TrySetResult();await worker.StopAsync(default);
    }
}
