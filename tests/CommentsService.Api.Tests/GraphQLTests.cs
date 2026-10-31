using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CommentsService.Api.Configuration;
using CommentsService.Api.GraphQL;
using CommentsService.Application.Comments;
using CommentsService.Application.Comments.GetRootPage;
using CommentsService.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed partial class CommentsHttpTests
{
    private const string PageQuery = "query Page($page:Int!,$sort:String!,$direction:String!){rootPage(page:$page,sort:$sort,direction:$direction){page pageSize totalCount totalPages items{id userName email homePage text createdAt parentId rootId image{id fileName width height} textFile{id fileName sizeBytes}}}}";

    [Theory]
    [InlineData("userName", "asc")] [InlineData("userName", "desc")]
    [InlineData("email", "asc")] [InlineData("email", "desc")]
    [InlineData("createdAt", "asc")] [InlineData("createdAt", "desc")]
    public async Task GraphQLPageMatchesRestAndUsesSameReadCache(string sort, string direction)
    {
        var rest = (await fixture.Client.GetFromJsonAsync<RootCommentsPage>($"/api/comments/page?page=1&sort={sort}&direction={direction}"))!;
        var cache = fixture.Factory.Services.GetRequiredService<CommentsService.Infrastructure.Caching.CommentReadCache>();
        var hits = cache.Statistics.Hits;
        using var response = await fixture.Client.PostAsJsonAsync("/api/graphql", new { query = PageQuery, variables = new { page = 1, sort, direction }, operationName = "Page" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.TryGetProperty("errors", out _), json.RootElement.ToString());
        var graph = json.RootElement.GetProperty("data").GetProperty("rootPage").Deserialize<RootCommentsPage>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.Equal(rest.Items.ToArray(), graph.Items.ToArray());
        Assert.Equal(rest.TotalCount, graph.TotalCount); Assert.Equal(25, graph.PageSize);
        Assert.True(cache.Statistics.Hits > hits);
    }

    [Fact]
    public async Task GraphQLCombinesSelectedCommentAndRepliesWithoutRecursiveFanOut()
    {
        using var rootResponse = await fixture.PostAsync(ApiFixture.Payload("GraphQL root"));
        var root = (await rootResponse.Content.ReadFromJsonAsync<CommentDto>())!;
        var input = ApiFixture.Payload("GraphQL reply"); input["parentId"] = root.Id;
        using var replyResponse = await fixture.PostAsync(input);
        var reply = (await replyResponse.Content.ReadFromJsonAsync<CommentDto>())!;
        using var response = await fixture.Client.PostAsJsonAsync("/api/graphql", new {
            query = "query($id:UUID!){comment(id:$id){id text} replies(parentId:$id,limit:1){items{id parentId} nextAfter}}", variables = new { id = root.Id }
        });
        var text = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(text);
        Assert.False(json.RootElement.TryGetProperty("errors", out _), text);
        var data = json.RootElement.GetProperty("data");
        Assert.Equal(root.Id, data.GetProperty("comment").GetProperty("id").GetGuid());
        Assert.Equal(reply.Id, data.GetProperty("replies").GetProperty("items")[0].GetProperty("id").GetGuid());
        Assert.Equal(2, data.GetProperty("comment").EnumerateObject().Count());
        Assert.DoesNotContain("userAgent", text); Assert.DoesNotContain("ipAddress", text);
        // The committed post invalidates the same cache used by GraphQL.
        using var pageResponse = await fixture.Client.PostAsJsonAsync("/api/graphql", new { query = "{rootPage{items{id text}}}" });
        Assert.Contains(root.Id.ToString(), await pageResponse.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("{rootPage(page:0){page}}")]
    [InlineData("{rootPage(sort:\"unsafe\"){page}}")]
    [InlineData("{replies(parentId:\"00000000-0000-0000-0000-000000000000\",limit:101){nextAfter}}")]
    [InlineData("{comment(id:\"not-a-uuid\"){id}}")]
    [InlineData("{rootPage{items{ipAddress}}}")]
    [InlineData("mutation{createComment{text}}")]
    [InlineData("subscription{commentsChanged}")]
    [InlineData("query A {rootPage{page}} query B {rootPage{page}}")]
    public async Task GraphQLRejectsInvalidArgumentsPrivateFieldsAndUnsupportedOperations(string query)
    {
        using var response = await fixture.Client.PostAsJsonAsync("/api/graphql", new { query });
        var text = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(text);
        Assert.True(json.RootElement.TryGetProperty("errors", out _), text);
        Assert.DoesNotContain("stackTrace", text); Assert.DoesNotContain("Npgsql", text);
    }

    [Theory]
    [InlineData("[]")] [InlineData("{\"query\":\"{__typename}\",\"variables\":[]}")]
    [InlineData("{\"query\":\"{__typename}\",\"extensions\":{}}")]
    [InlineData("{\"query\":\"{__typename}\",\"query\":\"{__typename}\"}")]
    [InlineData("{\"Query\":\"{__typename}\"}")]
    [InlineData("{\"query\":null}")]
    [InlineData("{\"query\":\" \"}")]
    public async Task GraphQLRejectsAmbiguousAndBatchedEnvelopes(string body)
    {
        using var response = await fixture.Client.PostAsync("/api/graphql", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GraphQLBoundsHttpBodyCostAndConcurrencyAndReleasesCapacity()
    {
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await fixture.Client.GetAsync("/api/graphql?sdl")).StatusCode);
        using var plain = await fixture.Client.PostAsync("/api/graphql", new StringContent("{__typename}"));
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, plain.StatusCode);
        using var big = await fixture.Client.PostAsJsonAsync("/api/graphql", new { query = new string(' ', 32769) });
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, big.StatusCode);
        var aliases = "{" + string.Join(" ", Enumerable.Range(0, 120).Select(i => $"a{i}:rootPage{{page}}")) + "}";
        using var expensive = await fixture.Client.PostAsJsonAsync("/api/graphql", new { query = aliases });
        Assert.Contains("errors", await expensive.Content.ReadAsStringAsync());
        var capacity = fixture.Factory.Services.GetRequiredService<GraphQLCapacity>();
        var held = 0;
        try
        {
            while (await capacity.Slots.WaitAsync(0)) held++;
            using var full = await fixture.Client.PostAsJsonAsync("/api/graphql", new { query = "{__typename}" });
            Assert.Equal(HttpStatusCode.TooManyRequests, full.StatusCode);
        }
        finally { capacity.Slots.Release(held); }
        using var recovered = await fixture.Client.PostAsJsonAsync("/api/graphql", new { query = "{__typename}" });
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
    }

    [Fact]
    public async Task GraphQLUnavailableStorageIsSanitizedAndRestSurvivesDisabledGraphQL()
    {
        await using var unavailable = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
            services.RemoveAll<CommentsService.Application.Comments.GetComment.IGetComment>();
            services.AddScoped<CommentsService.Application.Comments.GetComment.IGetComment, UnavailableQuery>();
        }));
        using var client = unavailable.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/graphql", new { query = "{comment(id:\"00000000-0000-0000-0000-000000000000\"){id}}" });
        var text = await response.Content.ReadAsStringAsync();
        Assert.Contains("STORAGE_UNAVAILABLE", text); Assert.DoesNotContain("private-database-details", text);
        Assert.DoesNotContain("exception", text, StringComparison.OrdinalIgnoreCase);
        await using var disabled = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
            var runtime = (RuntimeSettings)services.Single(item => item.ServiceType == typeof(RuntimeSettings)).ImplementationInstance!;
            runtime.GraphQL.Enabled = false;
        }));
        using var disabledClient = disabled.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await disabledClient.PostAsJsonAsync("/api/graphql", new { query = "{__typename}" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await disabledClient.GetAsync("/api/comments/page")).StatusCode);
        using var config = JsonDocument.Parse(await disabledClient.GetStringAsync("/api/config"));
        Assert.False(config.RootElement.GetProperty("graphQLEnabled").GetBoolean());
    }

    [Fact]
    public async Task GraphQLRouteVariantsCannotBypassTransportGuards()
    {
        foreach (var path in new[] { "/api/graphql", "/api/graphql/", "/API/GRAPHQL/" })
        {
            Assert.Equal(HttpStatusCode.MethodNotAllowed, (await fixture.Client.GetAsync(path)).StatusCode);
            using var array = await fixture.Client.PostAsync(path, new StringContent("[]", Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.BadRequest, array.StatusCode);
            using var plain = await fixture.Client.PostAsync(path, new StringContent("{__typename}"));
            Assert.Equal(HttpStatusCode.UnsupportedMediaType, plain.StatusCode);
            using var large = await fixture.Client.PostAsJsonAsync(path, new { query = new string(' ', 32769) });
            Assert.Equal(HttpStatusCode.RequestEntityTooLarge, large.StatusCode);
        }
    }

    [Fact]
    public async Task GraphQLParserAndExecutionDeadlinesAreEnforced()
    {
        var tooMany = "{" + string.Join(" ", Enumerable.Range(0, 257).Select(i => $"a{i}:__typename")) + "}";
        using var parsed = await fixture.Client.PostAsJsonAsync("/api/graphql", new { query = tooMany });
        Assert.Contains("errors", await parsed.Content.ReadAsStringAsync());
        await using var slow = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services => {
            services.RemoveAll<CommentsService.Application.Comments.GetComment.IGetComment>();
            services.AddScoped<CommentsService.Application.Comments.GetComment.IGetComment, SlowGraphComment>();
            services.AddGraphQLServer().ModifyRequestOptions(options => options.ExecutionTimeout = TimeSpan.FromSeconds(1));
        }));
        using var client = slow.CreateClient();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        using var response = await client.PostAsJsonAsync("/api/graphql", new { query = "{comment(id:\"00000000-0000-0000-0000-000000000000\"){id}}" });
        Assert.Contains("errors", await response.Content.ReadAsStringAsync());
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(4));
    }

    private sealed class SlowGraphComment : CommentsService.Application.Comments.GetComment.IGetComment
    {
        public async Task<CommentDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            return null;
        }
    }

    [Theory]
    [InlineData("MaximumDepth", "0")] [InlineData("MaximumFields", "0")]
    [InlineData("MaximumRequestBytes", "1048577")] [InlineData("ExecutionTimeoutSeconds", "0")]
    [InlineData("MaximumFieldCost", "0")] [InlineData("MaximumTypeCost", "0")]
    [InlineData("MaximumConcurrentRequests", "0")]
    public void InvalidGraphQLConfigurationFailsStartup(string key, string value)
    {
        Assert.Throws<InvalidOperationException>(() => RuntimeConfiguration.Load(new Microsoft.Extensions.Configuration.ConfigurationManager(), [$"--Runtime:GraphQL:{key}={value}"]));
    }
}
