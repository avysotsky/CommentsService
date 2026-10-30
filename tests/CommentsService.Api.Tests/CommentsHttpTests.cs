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
    public async Task PreviewAndCreateShareValidationAndPreviewDoesNotWrite()
    {
        const string text = "<strong>Привіт <i>😀</i></strong> <a href=\"https://example.test/?a=1&amp;b=2\">link</a>";
        await using var db = new CommentsDbContextFactory().CreateDbContext([]);
        var count = await db.Comments.CountAsync();
        using var preview = await fixture.Client.PostAsJsonAsync("/api/comments/preview", new { text });
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        using var json = JsonDocument.Parse(await preview.Content.ReadAsStringAsync());
        Assert.Contains("<strong>", json.RootElement.GetProperty("html").GetString());
        Assert.Equal(count, await db.Comments.CountAsync());
        using var created = await fixture.PostAsync(ApiFixture.Payload(text));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var value = (await created.Content.ReadFromJsonAsync<CommentDto>())!;
        Assert.Equal(text, (await fixture.Client.GetFromJsonAsync<CommentDto>("/api/comments/" + value.Id))!.Text);
    }

    [Theory]
    [InlineData("<i>x</strong>")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>")]
    [InlineData("<code onmouseover=\"bad\">x</code>")]
    public async Task PreviewAndCreateRejectTheSameInvalidBody(string text)
    {
        using var preview = await fixture.Client.PostAsJsonAsync("/api/comments/preview", new { text });
        using var created = await fixture.PostAsync(ApiFixture.Payload(text));
        Assert.Equal(HttpStatusCode.BadRequest, preview.StatusCode);
        Assert.Equal(preview.StatusCode, created.StatusCode);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"text\":\"hello\",\"html\":\"spoofed\"}")]
    public async Task PreviewRejectsMissingOrUnknownFields(string body)
    {
        using var response = await fixture.Client.PostAsync("/api/comments/preview", new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

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
        Assert.Equal(new[] { "createdAt", "email", "homePage", "id", "parentId", "rootId", "text", "userName" },
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
    [InlineData("text", "<strong onclick=\"bad\">Forbidden attribute</strong>")]
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
    [InlineData("rootId")]
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

    private async Task<CommentDto> CreateThreadComment(Guid? parentId = null, string text = "Thread fixture")
    {
        var payload = ApiFixture.Payload(text); payload["parentId"] = parentId;
        using var response = await fixture.PostAsync(payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CommentDto>())!;
    }

    [Fact]
    public async Task NestedRepliesPersistParentAndDerivedRootAndAreNotListedAsRoots()
    {
        var root = await CreateThreadComment(); var parent = root;
        Assert.Null(root.ParentId); Assert.Equal(root.Id, root.RootId);
        for (var depth = 0; depth < 40; depth++)
        {
            var child = await CreateThreadComment(parent.Id, "<strong>Reply 😀</strong>");
            Assert.Equal(parent.Id, child.ParentId); Assert.Equal(root.Id, child.RootId);
            Assert.Equal(child, await fixture.Client.GetFromJsonAsync<CommentDto>("/api/comments/" + child.Id));
            var page = await fixture.Client.GetFromJsonAsync<CommentsService.Application.Comments.GetReplies.RepliesPage>("/api/comments/" + parent.Id + "/replies");
            Assert.Equal(child.Id, Assert.Single(page!.Items).Id); Assert.Null(page.NextAfter); parent = child;
        }
        var roots = (await fixture.Client.GetFromJsonAsync<CommentDto[]>("/api/comments?limit=100"))!;
        Assert.All(roots, item => Assert.Null(item.ParentId)); Assert.Contains(roots, item => item.Id == root.Id);
    }

    [Fact]
    public async Task ReplyCursorPagesDoNotMixParentsOrDuplicateRows()
    {
        var root = await CreateThreadComment(); var other = await CreateThreadComment();
        var otherChild = await CreateThreadComment(other.Id);
        var children = new List<CommentDto>();
        for (var i = 0; i < 5; i++) children.Add(await CreateThreadComment(root.Id));
        Guid? after = null; var found = new List<Guid>();
        do
        {
            var page = (await fixture.Client.GetFromJsonAsync<CommentsService.Application.Comments.GetReplies.RepliesPage>(
                $"/api/comments/{root.Id}/replies?limit=2" + (after is null ? "" : "&after=" + after)))!;
            Assert.All(page.Items, child => Assert.Equal(root.Id, child.ParentId));
            found.AddRange(page.Items.Select(x => x.Id)); after = page.NextAfter;
        } while (after is not null);
        Assert.Equal(children.AsEnumerable().Reverse().Select(x => x.Id), found);
        using var foreign = await fixture.Client.GetAsync($"/api/comments/{root.Id}/replies?after={otherChild.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
    }

    [Fact]
    public async Task MissingReplyTargetDoesNotWriteAndMissingBranchIsNotFound()
    {
        await using var db = new CommentsDbContextFactory().CreateDbContext([]);
        var authors = await db.Authors.CountAsync(); var comments = await db.Comments.CountAsync();
        var missing = Guid.NewGuid(); var payload = ApiFixture.Payload(); payload["parentId"] = missing;
        using var response = await fixture.PostAsync(payload);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("parentId", out _));
        Assert.Equal(authors, await db.Authors.CountAsync()); Assert.Equal(comments, await db.Comments.CountAsync());
        using var read = await fixture.Client.GetAsync($"/api/comments/{missing}/replies");
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
    }

    [Theory]
    [InlineData("limit=0")] [InlineData("limit=101")] [InlineData("limit=bad")] [InlineData("after=bad")]
    public async Task InvalidReplyParametersAreRejected(string query)
    {
        var root = await CreateThreadComment();
        using var response = await fixture.Client.GetAsync($"/api/comments/{root.Id}/replies?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task EmptyBranchAndExplicitNullParentAreValid()
    {
        var root = await CreateThreadComment();
        var page = (await fixture.Client.GetFromJsonAsync<CommentsService.Application.Comments.GetReplies.RepliesPage>($"/api/comments/{root.Id}/replies"))!;
        Assert.Empty(page.Items); Assert.Null(page.NextAfter);
    }

    [Theory]
    [InlineData("page=0")] [InlineData("page=-1")] [InlineData("page=85899346")]
    [InlineData("page=bad")] [InlineData("sort=Text")] [InlineData("sort=email%3BDROP%20TABLE")]
    [InlineData("direction=descending")]
    public async Task RootPagingRejectsInvalidQueries(string query)
    {
        using var response = await fixture.Client.GetAsync("/api/comments/page?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task RootPagingDefaultsToLifoAndCountsOnlyRoots()
    {
        var first = await CreateThreadComment(text: "First root page");
        var second = await CreateThreadComment(text: "Newest root page");
        var reply = await CreateThreadComment(first.Id);
        var page = (await fixture.Client.GetFromJsonAsync<CommentsService.Application.Comments.GetRootPage.RootCommentsPage>("/api/comments/page"))!;
        Assert.Equal(1, page.Page); Assert.Equal(25, page.PageSize);
        Assert.Equal(second.Id, page.Items[0].Id);
        Assert.DoesNotContain(page.Items, x => x.Id == reply.Id); Assert.All(page.Items, x => Assert.Null(x.ParentId));
        Assert.True(page.TotalCount >= 2); Assert.Equal((page.TotalCount + 24) / 25, page.TotalPages);
        using var response = await fixture.Client.GetAsync("/api/comments/page?sort=email&direction=asc&page=85899345");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.Content.ReadFromJsonAsync<CommentsService.Application.Comments.GetRootPage.RootCommentsPage>())!.Items);
    }

    private sealed class UnavailableQuery : IGetComment
    {
        public Task<CommentDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromException<CommentDto?>(new StorageUnavailableException("private-database-details"));
    }
}
