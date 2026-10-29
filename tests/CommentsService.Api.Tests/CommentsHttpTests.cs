using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CommentsService.Application.Comments;
using CommentsService.Application.Comments.GetComment;
using CommentsService.Application.Common;
using CommentsService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed class CommentsHttpTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task CreateAndReadPreserveUnicodeAndCaptureServerMetadata()
    {
        var payload = ApiFixture.Payload("  Привіт / Привет / مرحبا / 😀\nДругий рядок ' OR '1'='1  ");
        var expectedName = (string)payload["userName"]!;
        payload["userName"] = " " + expectedName + " ";
        payload["homePage"] = " https://example.test/path?q=1 ";
        var before = DateTimeOffset.UtcNow;
        using var response = await fixture.PostAsync(payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<CommentDto>())!;
        Assert.Equal($"/api/comments/{created.Id}", response.Headers.Location!.OriginalString);
        Assert.Equal(expectedName, created.UserName);
        Assert.Equal(payload["text"], created.Text);
        Assert.Equal("https://example.test/path?q=1", created.HomePage);
        Assert.Equal(TimeSpan.Zero, created.CreatedAt.Offset);
        Assert.InRange(created.CreatedAt, before.AddMilliseconds(-1), DateTimeOffset.UtcNow);
        using var get = await fixture.Client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(created, await get.Content.ReadFromJsonAsync<CommentDto>());
        using var document = JsonDocument.Parse(await get.Content.ReadAsStringAsync());
        Assert.Equal(new[] { "createdAt", "email", "homePage", "id", "text", "userName" },
            document.RootElement.EnumerateObject().Select(property => property.Name).Order().ToArray());
        await using var db = new CommentsDbContextFactory().CreateDbContext([]);
        var stored = await db.Comments.AsNoTracking().SingleAsync(comment => comment.Id == created.Id);
        Assert.Equal("CommentsService.Api.Tests/1.0", stored.UserAgent);
        Assert.NotEqual(IPAddress.Parse("203.0.113.79"), stored.ClientIp);
    }

    [Fact]
    public async Task RecentListIsBoundedAndNewestFirst()
    {
        using var first = await fixture.PostAsync(ApiFixture.Payload("First list fixture"));
        using var second = await fixture.PostAsync(ApiFixture.Payload("Second list fixture"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var recent = (await fixture.Client.GetFromJsonAsync<CommentDto[]>("/api/comments?limit=2"))!;
        Assert.Equal(2, recent.Length);
        Assert.True(recent[0].CreatedAt >= recent[1].CreatedAt);
        var defaults = (await fixture.Client.GetFromJsonAsync<CommentDto[]>("/api/comments"))!;
        Assert.InRange(defaults.Length, 2, 25);
    }

    [Theory]
    [InlineData("userName", "")]
    [InlineData("userName", "Ім’я")]
    [InlineData("userName", "with-dash")]
    [InlineData("email", "invalid")]
    [InlineData("email", "Reader <reader@example.test>")]
    [InlineData("homePage", "javascript:alert(1)")]
    [InlineData("homePage", "ftp://example.test/")]
    [InlineData("text", " ")]
    [InlineData("text", "<script>alert(1)</script>")]
    [InlineData("text", "<strong>Not enabled yet</strong>")]
    [InlineData("text", "bad\0text")]
    public async Task InvalidFieldsReturnValidationProblems(string field, string value)
    {
        var payload = ApiFixture.Payload(); payload[field] = value;
        using var response = await fixture.PostAsync(payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Theory]
    [InlineData("createdAt")]
    [InlineData("clientIp")]
    [InlineData("userAgent")]
    [InlineData("parentId")]
    public async Task ClientCannotSupplyServerMetadataOrUnimplementedFields(string field)
    {
        var payload = ApiFixture.Payload(); payload[field] = "spoofed";
        using var response = await fixture.PostAsync(payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task TextAtTheStorageBoundaryIsAcceptedAndLongerTextRejected()
    {
        using var valid = await fixture.PostAsync(ApiFixture.Payload(new string('a', 65536)));
        Assert.Equal(HttpStatusCode.Created, valid.StatusCode);
        using var invalid = await fixture.PostAsync(ApiFixture.Payload(new string('a', 65537)));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"userName\":123}")]
    public async Task InvalidJsonOrMissingFieldsReturnBadRequest(string json)
    {
        using var response = await fixture.SendAsync(new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LongUserAgentIsBoundedWithoutRejectingValidText()
    {
        using var response = await fixture.SendAsync(JsonContent.Create(ApiFixture.Payload("Metadata limit fixture")), new string('A', 3000));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<CommentDto>())!;
        await using var db = new CommentsDbContextFactory().CreateDbContext([]);
        var stored = await db.Comments.AsNoTracking().SingleAsync(comment => comment.Id == created.Id);
        Assert.Equal(new string('A', 2048), stored.UserAgent);
    }

    [Fact]
    public async Task NonJsonContentIsUnsupported()
    {
        using var response = await fixture.SendAsync(new StringContent("Text", Encoding.UTF8, "text/plain"));
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("101")]
    [InlineData("invalid")]
    public async Task InvalidListLimitsReturnBadRequest(string limit)
    {
        using var response = await fixture.Client.GetAsync("/api/comments?limit=" + limit);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MissingCommentReturnsNotFound()
    {
        using var response = await fixture.Client.GetAsync("/api/comments/" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task StorageErrorsReturnGenericUnavailableProblem()
    {
        await using var factory = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGetComment>();
            services.AddScoped<IGetComment, UnavailableQuery>();
        }));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/comments/" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("private-database-details", text);
        Assert.DoesNotContain("stack", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RealConnectionFailureReturnsUnavailableWithoutProviderDetails()
    {
        await using var factory = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<CommentsDbContext>();
            services.AddScoped(_ => new CommentsDbContext(new DbContextOptionsBuilder<CommentsDbContext>()
                .UseNpgsql("Host=127.0.0.1;Port=1;Database=unreachable;Username=unavailable;Timeout=1").Options));
        }));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/comments");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("127.0.0.1", text);
        Assert.DoesNotContain("Npgsql", text);
        Assert.DoesNotContain("password", text, StringComparison.OrdinalIgnoreCase);
        using var write = await client.PostAsJsonAsync("/api/comments", ApiFixture.Payload("Unavailable write fixture"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, write.StatusCode);
        Assert.DoesNotContain("Npgsql", await write.Content.ReadAsStringAsync());
    }

    private sealed class UnavailableQuery : IGetComment
    {
        public Task<CommentDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromException<CommentDto?>(new StorageUnavailableException("private-database-details"));
    }
}
