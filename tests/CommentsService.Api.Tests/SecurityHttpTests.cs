using System.Net;
using System.Net.Http.Json;
using System.Text;
using CommentsService.Application.Comments;
using CommentsService.Application.Comments.GetComment;
using CommentsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed partial class CommentsHttpTests
{
    [Theory]
    [InlineData("/api/status", 200)]
    [InlineData("/api/comments/page", 200)]
    [InlineData("/api/captcha", 200)]
    [InlineData("/api/unknown", 404)]
    [InlineData("/AGENTS.md", 404)]
    [InlineData("/.local/docker.env", 404)]
    public async Task SecurityHeadersCoverSuccessAndFailure(string path, int status)
    {
        using var response = await fixture.Client.GetAsync(path);
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("script-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
        Assert.DoesNotContain("unsafe-inline", csp);
        Assert.DoesNotContain("unsafe-eval", csp);
        if (path.StartsWith("/api/", StringComparison.Ordinal)) Assert.True(response.Headers.CacheControl!.NoStore);
    }

    [Theory]
    [InlineData("/api/comments", "{\"text\":\"first\",\"text\":\"second\"}")]
    [InlineData("/api/comments/preview", "{\"text\":\"first\",\"text\":\"second\"}")]
    [InlineData("/api/comments/preview", "{\"text\":\"first\",\"Text\":\"second\"}")]
    [InlineData("/api/comments/preview", "{\"Text\":\"hello\"}")]
    [InlineData("/api/comments/preview", "{\"text\":\"x\",\"rootId\":\"spoof\"}")]
    public async Task AmbiguousOrUnknownJsonIsRejectedWithoutWrites(string path, string json)
    {
        await using var db = new CommentsDbContextFactory().CreateDbContext([]);
        var count = await db.Comments.CountAsync();
        using var response = await fixture.Client.PostAsync(path, new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(count, await db.Comments.CountAsync());
    }

    [Theory]
    [InlineData("/api/comments", false)] [InlineData("/api/comments", true)]
    [InlineData("/api/comments/preview", false)] [InlineData("/api/comments/preview", true)]
    public async Task JsonByteLimitAppliesWithAndWithoutContentLength(string path, bool chunked)
    {
        var bytes = Encoding.UTF8.GetBytes(new string(' ', 1024 * 1024) + "{\"text\":\"hello\"}");
        using HttpContent body = chunked ? new UnknownLengthJson(bytes) : new ByteArrayContent(bytes);
        body.Headers.ContentType = new("application/json");
        using var response = await fixture.Client.PostAsync(path, body);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Contains("Request body is too large.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ExactlyOneMiBOfUtf8JsonCanBePreviewed()
    {
        const string json = "{\"text\":\"hello\"}";
        using var body = new UnknownLengthJson(Encoding.UTF8.GetBytes(new string(' ', 1024 * 1024 - json.Length) + json));
        using var response = await fixture.Client.PostAsync("/api/comments/preview", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("text/plain")] [InlineData("application/x-www-form-urlencoded")]
    [InlineData("application/json; charset=utf-16")] [InlineData("application/json; charset=unknown")]
    public async Task UnsupportedEncodingsAndSimpleFormBodiesAreRejected(string contentType)
    {
        using var body = new ByteArrayContent(Encoding.UTF8.GetBytes("{\"text\":\"hello\"}"));
        body.Headers.TryAddWithoutValidation("Content-Type", contentType);
        using var response = await fixture.Client.PostAsync("/api/comments/preview", body);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task InvalidUtf8CannotBecomeAReplacementCharacterInJson()
    {
        using var body = new ByteArrayContent([123,34,116,101,120,116,34,58,34,255,34,125]);
        body.Headers.ContentType = new("application/json");
        using var response = await fixture.Client.PostAsync("/api/comments/preview", body);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("<svg><script>alert(1)</script></svg>")]
    [InlineData("<a href=\"java&#x09;script:alert(1)\">x</a>")]
    [InlineData("<a href=\"data:text/html,x\">x</a>")]
    [InlineData("<a href=\"https://example.test\" style=\"color:red\">x</a>")]
    [InlineData("<a href=\"https://example.test\" target=\"_blank\">x</a>")]
    [InlineData("<code xmlns:x=\"urn:evil\" x:onload=\"run()\">x</code>")]
    [InlineData("<!DOCTYPE root [<!ENTITY x SYSTEM 'file:///security-review-never-read'>]>&x;")]
    public async Task XssAndXmlPayloadsFailPreviewAndCreateWithoutWrites(string text)
    {
        await using var db = new CommentsDbContextFactory().CreateDbContext([]);
        var comments = await db.Comments.CountAsync(); var authors = await db.Authors.CountAsync();
        using var preview = await fixture.Client.PostAsJsonAsync("/api/comments/preview", new { text });
        using var create = await fixture.PostAsync(ApiFixture.Payload(text));
        Assert.Equal(HttpStatusCode.BadRequest, preview.StatusCode);
        Assert.Equal(preview.StatusCode, create.StatusCode);
        Assert.Equal(comments, await db.Comments.CountAsync());
        Assert.Equal(authors, await db.Authors.CountAsync());
    }

    [Theory]
    [InlineData("sort=createdAt%3BSELECT%201")]
    [InlineData("direction=desc%27%20OR%20%271%27%3D%271")]
    [InlineData("page=1%20OR%201%3D1")]
    public async Task SqlLikePagingParametersAreRejected(string query)
    {
        using var response = await fixture.Client.GetAsync("/api/comments/page?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SqlLikeCommentAndApostropheEmailRemainLiteralData()
    {
        const string text = "' OR '1'='1 --; SELECT 1;";
        var payload = ApiFixture.Payload(text); payload["email"] = "o'connor@example.test";
        using var response = await fixture.PostAsync(payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<CommentDto>())!;
        var read = (await fixture.Client.GetFromJsonAsync<CommentDto>("/api/comments/" + created.Id))!;
        Assert.Equal(text, read.Text); Assert.Equal(payload["email"], read.Email);
    }

    [Fact]
    public async Task UnexpectedErrorsRetainHeadersAndDoNotExposeDetails()
    {
        await using var factory = fixture.Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGetComment>(); services.AddScoped<IGetComment, SecurityFailingQuery>();
        }));
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/comments/" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("private-security-test-details", await response.Content.ReadAsStringAsync());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    private sealed class SecurityFailingQuery : IGetComment
    {
        public Task<CommentDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromException<CommentDto?>(new InvalidOperationException("private-security-test-details"));
    }

    private sealed class UnknownLengthJson : HttpContent
    {
        private readonly byte[] bytes;
        public UnknownLengthJson(byte[] bytes) { this.bytes = bytes; Headers.ContentType = new("application/json"); }
        protected override bool TryComputeLength(out long length) { length = 0; return false; }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) => stream.WriteAsync(bytes).AsTask();
    }
}
