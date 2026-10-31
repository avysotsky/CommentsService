using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CommentsService.Application.Comments;
using CommentsService.Application.Common;
using CommentsService.Application.Search;
using CommentsService.Infrastructure.Search;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed partial class CommentsHttpTests
{
    [Fact]
    public async Task SearchIsOptionalAndUnavailableSearchDoesNotBreakOriginalEndpoints()
    {
        Assert.Equal(HttpStatusCode.NotFound,(await fixture.Client.GetAsync("/api/comments/search?query=test")).StatusCode);
        var index=new SearchIndexFake { Fail=true };
        await using var configured=fixture.Factory.WithWebHostBuilder(builder=>builder.ConfigureServices(services=>{
            var runtime=(CommentsService.Api.Configuration.RuntimeSettings)services.Single(s=>s.ServiceType==typeof(CommentsService.Api.Configuration.RuntimeSettings)).ImplementationInstance!;
            runtime.Search.Enabled=true;
            services.RemoveAll<ICommentSearchIndex>(); services.AddSingleton<ICommentSearchIndex>(index);
        }));
        using var client=configured.CreateClient();
        using var failed=await client.GetAsync("/api/comments/search?query=test");
        Assert.Equal(HttpStatusCode.ServiceUnavailable,failed.StatusCode);
        Assert.Contains("Comment search is unavailable",await failed.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/comments/page")).StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/captcha")).StatusCode);
        using var graph=await client.PostAsJsonAsync("/api/graphql",new {query="{rootPage{page pageSize}}"});
        Assert.DoesNotContain("errors",await graph.Content.ReadAsStringAsync());
        using var saved=await fixture.PostAsync(ApiFixture.Payload("Search unavailable preservation"));
        Assert.Equal(HttpStatusCode.Created,saved.StatusCode);
    }

    [Fact]
    public async Task SearchHydratesCurrentSqlDataAndBoundsConcurrencyAndArguments()
    {
        using var posted=await fixture.PostAsync(ApiFixture.Payload("Canonical search text"));
        var comment=(await posted.Content.ReadFromJsonAsync<CommentDto>())!;
        var index=new SearchIndexFake { Ids=[comment.Id,Guid.NewGuid()] };
        await using var configured=fixture.Factory.WithWebHostBuilder(builder=>builder.ConfigureServices(services=>{
            var runtime=(CommentsService.Api.Configuration.RuntimeSettings)services.Single(s=>s.ServiceType==typeof(CommentsService.Api.Configuration.RuntimeSettings)).ImplementationInstance!;
            runtime.Search.Enabled=true;
            services.RemoveAll<ICommentSearchIndex>();services.AddSingleton<ICommentSearchIndex>(index);
        }));
        using var client=configured.CreateClient();
        using var response=await client.GetAsync("/api/comments/search?query=Canonical&page=1");
        Assert.Equal(HttpStatusCode.OK,response.StatusCode); Assert.True(response.Headers.CacheControl!.NoStore);
        var data=(await response.Content.ReadFromJsonAsync<SearchPage>())!;
        Assert.Equal(comment,Assert.Single(data.Items)); Assert.Equal(10,data.PageSize);
        foreach(var query in new[]{"",new string('x',201),"hello%00"})
            Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/comments/search?query="+query)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync("/api/comments/search?query=x&page=101")).StatusCode);
        var slots=configured.Services.GetRequiredService<CommentsService.Api.Search.SearchCapacity>().Slots; var held=0;
        try { while(await slots.WaitAsync(0)) held++;Assert.Equal(HttpStatusCode.TooManyRequests,(await client.GetAsync("/api/comments/search?query=x")).StatusCode); }
        finally { slots.Release(held); }
        Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/comments/search?query=x")).StatusCode);
    }
    private sealed class SearchIndexFake : ICommentSearchIndex
    {
        public bool Fail; public Guid[] Ids=[];
        public Task<SearchIds> SearchAsync(string query,int page,int pageSize,CancellationToken token)=>Fail?Task.FromException<SearchIds>(new SearchUnavailableException()):Task.FromResult(new SearchIds(Ids,true));
        public Task IndexAsync(IReadOnlyList<SearchDocument> documents,CancellationToken token)=>Task.CompletedTask;
        public Task<IReadOnlyList<Guid>> ScanIdsAsync(Guid? after,int limit,CancellationToken token)=>Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task DeleteAsync(IReadOnlyList<Guid> ids,CancellationToken token)=>Task.CompletedTask;
    }
}

public sealed class SearchPolicyTests
{
    [Fact]
    public void DatabasesCannotShareAnIndexEvenWithIdenticalConfigPrefixes()
    {
        var names=new[]{"comments","comments_test","comments_benchmark"}.Select(db=>SearchIndexName.ForDatabase("comments-v1",db)).ToArray();
        Assert.Equal(3,names.Distinct().Count());
        Assert.Equal(names[0],SearchIndexName.ForDatabase("comments-v1","comments"));
        Assert.All(names,name=>Assert.Matches("^comments-v1-[0-9a-f]{12}$",name));
    }

    [Theory]
    [InlineData("PageSize","0")] [InlineData("PageSize","26")] [InlineData("MaximumQueryLength","501")]
    [InlineData("MaximumPage","101")] [InlineData("TimeoutSeconds","6")] [InlineData("MaximumConcurrentRequests","9")]
    [InlineData("BatchSize","101")] [InlineData("BatchDelayMilliseconds","0")] [InlineData("ReconcileIntervalSeconds","0")]
    [InlineData("FailureRetrySeconds","0")] [InlineData("Capacity","0")] [InlineData("MaximumIndexBytes","2147483649")]
    [InlineData("Endpoint","file:///private")] [InlineData("Index","other-project")]
    public void InvalidSearchPoliciesFailWithoutPrintingValues(string key,string value)
    {
        var error=Assert.Throws<InvalidOperationException>(()=>CommentsService.Api.Configuration.RuntimeConfiguration.Load(new Microsoft.Extensions.Configuration.ConfigurationManager(),[$"--Runtime:Search:{key}={value}"]));
        Assert.Equal("Runtime configuration could not be loaded or validated.",error.Message);
    }
    [Theory]
    [InlineData("<strong>Привіт</strong> <i>мир</i>","Привіт  мир")]
    [InlineData("plain 1 < 2 & literal","plain 1 < 2 & literal")]
    [InlineData("<a href=\"https://example.test/private\">visible</a>","visible")]
    public void SearchTextIndexesVisibleWordsWithoutAttributes(string input,string expected)=>Assert.Equal(expected,SearchDocumentText.Extract(input));
    [Fact]
    public void SignalBufferBoundsMemoryAndPreservesRecentIds()
    {
        var signals=new SearchSignals(new(){Capacity=2});var last=Guid.NewGuid();
        for(var i=0;i<100;i++)signals.Notify(Guid.NewGuid());signals.Notify(last);
        var ids=signals.Drain(100);Assert.Equal(2,ids.Length);Assert.Contains(last,ids);Assert.Empty(signals.Drain(100));
    }
}

public sealed class ElasticAdapterTests : IDisposable
{
    private readonly string? previous=Environment.GetEnvironmentVariable("SEARCH_PASSWORD");
    public ElasticAdapterTests()=>Environment.SetEnvironmentVariable("SEARCH_PASSWORD","search-unit-secret");
    public void Dispose()=>Environment.SetEnvironmentVariable("SEARCH_PASSWORD",previous);
    private sealed class Handler(Func<HttpRequestMessage,string,Task<HttpResponseMessage>> respond):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>await respond(request,request.Content is null?"":await request.Content.ReadAsStringAsync(token));
    }
    private static HttpResponseMessage Json(object value)=>new(HttpStatusCode.OK){Content=JsonContent.Create(value)};
    [Fact]
    public async Task AdapterUsesLiteralQueryFixedFieldsNoSourceAndStableIds()
    {
        var id=Guid.NewGuid();var calls=0;
        using var http=new HttpClient(new Handler((request,body)=>{
            calls++;Assert.Equal("Basic",request.Headers.Authorization!.Scheme);
            if(request.RequestUri!.AbsolutePath.EndsWith("_mapping"))return Task.FromResult(Json(new Dictionary<string,object>{{"comments-v1",new{mappings=new{_meta=new{commentSchema=1}}}}}));
            using var parsed=JsonDocument.Parse(body);var root=parsed.RootElement;
            Assert.False(root.GetProperty("_source").GetBoolean());Assert.Equal(11,root.GetProperty("size").GetInt32());
            var match=root.GetProperty("query").GetProperty("multi_match");Assert.Equal("{\"match_all\":{}} *",match.GetProperty("query").GetString());
            Assert.Equal(new[]{"text","userName^2"},match.GetProperty("fields").EnumerateArray().Select(x=>x.GetString()));
            return Task.FromResult(Json(new{timed_out=false,hits=new{hits=new[]{new{_id=id.ToString()}}}}));
        }));
        using var index=new ElasticCommentIndex(new(){},http);
        var result=await index.SearchAsync("{\"match_all\":{}} *",1,10,default);
        Assert.Equal(id,Assert.Single(result.Items));Assert.Equal(2,calls);
    }
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"timed_out\":true,\"hits\":{\"hits\":[]}}")]
    [InlineData("{\"timed_out\":false,\"hits\":{\"hits\":[{\"_id\":\"invalid\"}]}}")]
    public async Task MalformedOrPartialProviderResultsAreUnavailable(string body)
    {
        using var http=new HttpClient(new Handler((request,_)=>request.RequestUri!.AbsolutePath.EndsWith("_mapping")
            ? Task.FromResult(Json(new Dictionary<string,object>{{"comments-v1",new{mappings=new{_meta=new{commentSchema=1}}}}}))
            : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(body,Encoding.UTF8,"application/json")})));
        using var index=new ElasticCommentIndex(new(),http);
        await Assert.ThrowsAsync<SearchUnavailableException>(()=>index.SearchAsync("hello",1,10,default));
    }

    [Theory]
    [InlineData(401)] [InlineData(403)] [InlineData(429)] [InlineData(500)] [InlineData(503)]
    public async Task ProviderFailuresBecomeSearchOnlyUnavailable(int status)
    {
        using var http=new HttpClient(new Handler((_,_)=>Task.FromResult(new HttpResponseMessage((HttpStatusCode)status){Content=new StringContent("private provider password")})));
        using var index=new ElasticCommentIndex(new(),http);
        var e=await Assert.ThrowsAsync<SearchUnavailableException>(()=>index.SearchAsync("hello",1,10,default));Assert.DoesNotContain("private",e.Message);
    }
    [Fact]
    public async Task BulkHasOnlyPublicSearchFieldsAndChecksSoftDiskBudget()
    {
        var uploads=new List<string>();var used=0L;
        using var http=new HttpClient(new Handler((request,body)=>{
            var path=request.RequestUri!.AbsolutePath;
            if(path.EndsWith("_mapping"))return Task.FromResult(Json(new Dictionary<string,object>{{"comments-v1",new{mappings=new{_meta=new{commentSchema=1}}}}}));
            if(path.EndsWith("_stats/store"))return Task.FromResult(Json(new{_all=new{primaries=new{store=new{size_in_bytes=used}}}}));
            uploads.Add(body);return Task.FromResult(Json(new{errors=false}));
        }));
        using var index=new ElasticCommentIndex(new(){MaximumIndexBytes=1048576},http);
        await index.IndexAsync([new(Guid.NewGuid(),"Reader","<strong>Привіт</strong>",DateTimeOffset.UtcNow)],default);
        var bulk=Assert.Single(uploads);Assert.EndsWith("\n",bulk);
        foreach(var name in new[]{"email","ipAddress","userAgent","homePage","textFile","image"})Assert.DoesNotContain(name,bulk);
        Assert.DoesNotContain("<strong>",bulk);used=1048576;
        await Assert.ThrowsAsync<SearchUnavailableException>(()=>index.IndexAsync([new(Guid.NewGuid(),"Reader","text",DateTimeOffset.UtcNow)],default));
        Assert.Single(uploads);
    }
}
