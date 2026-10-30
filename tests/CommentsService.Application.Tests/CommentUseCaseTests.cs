using System.Net;
using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Comments;
using CommentsService.Application.Comments.CreateComment;
using CommentsService.Application.Comments.GetComment;
using CommentsService.Application.Comments.GetLatestComments;
using CommentsService.Application.Common;
using CommentsService.Domain.Entities;
using Xunit;

namespace CommentsService.Application.Tests;

public sealed class CommentUseCaseTests
{
    private static readonly ClientMetadata Client = new(IPAddress.IPv6Loopback, "Application tests");
    private static CreateCommentInput Input => new("Reader123", "reader@example.test", null, "Hello");

    public static IEnumerable<object?[]> InvalidFields()
    {
        foreach (var value in new string?[] { null, "", " ", "Ім’я", "name-with-dash" }) yield return ["userName", value];
        foreach (var value in new string?[] { null, "", "not-an-email", "Reader <reader@example.test>", "a@example.test\r\nBcc: x@example.test" }) yield return ["email", value];
        foreach (var value in new[] { "javascript:alert(1)", "ftp://example.test/", "/relative", "https://user:password@example.test/", "https://example.test/\npath" }) yield return ["homePage", value];
        foreach (var value in new string?[] { null, "", " ", "<script>alert(1)</script>", "<i>Hi</strong>", "<!--comment-->", "<script", "bad\0text" }) yield return ["text", value];
    }

    [Theory]
    [MemberData(nameof(InvalidFields))]
    public async Task InvalidInputDoesNotStageOrSave(string field, string? value)
    {
        var fake = new Storage();
        var input = field switch
        {
            "userName" => Input with { UserName = value },
            "email" => Input with { Email = value },
            "homePage" => Input with { HomePage = value },
            _ => Input with { Text = value }
        };
        var result = await new CreateComment(fake, fake, new CommentsService.Application.Comments.Markup.XhtmlCommentMarkup()).ExecuteAsync(input, Client);
        Assert.False(result.IsValid);
        Assert.Contains(field, result.Errors.Keys);
        Assert.Null(result.Value);
        Assert.Empty(fake.Comments);
        Assert.Equal(0, fake.SaveCalls);
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("Привіт / Привет / مرحبا / 😀\nДругий рядок")]
    [InlineData("  Text with preserved spaces  ")]
    [InlineData("1 < 2 && 3 > 1")]
    public async Task ValidTextIsPreservedAndMetadataComesFromContext(string text)
    {
        var fake = new Storage();
        using var cancellation = new CancellationTokenSource();
        var result = await new CreateComment(fake, fake, new CommentsService.Application.Comments.Markup.XhtmlCommentMarkup()).ExecuteAsync(Input with
            { UserName = " Reader123 ", Email = " reader@example.test ", HomePage = "   ", Text = text }, Client, cancellation.Token);
        Assert.True(result.IsValid);
        var saved = Assert.Single(fake.Comments);
        Assert.Equal("Reader123", saved.Author.UserName);
        Assert.Equal("reader@example.test", saved.Author.Email);
        Assert.Null(saved.Author.HomePage);
        Assert.Equal(text, saved.Text);
        Assert.Equal(Client.IpAddress, saved.ClientIp);
        Assert.Equal(Client.UserAgent, saved.UserAgent);
        Assert.Equal(text, result.Value!.Text);
        Assert.Equal(TimeSpan.Zero, result.Value.CreatedAt.Offset);
        Assert.Equal(0, result.Value.CreatedAt.Ticks % 10);
        Assert.Equal(cancellation.Token, fake.Token);
        Assert.Equal(1, fake.SaveCalls);
    }

    [Fact]
    public async Task LimitsAndInvalidUnicodeAreRejectedBeforePersistence()
    {
        foreach (var input in new[] { Input with { UserName = new string('A', 101) },
            Input with { Email = new string('a', 255) + "@example.test" },
            Input with { HomePage = "https://example.test/" + new string('a', 2048) },
            Input with { Text = new string('a', 65537) }, Input with { Text = new string((char)0xD800, 1) } })
        {
            var fake = new Storage();
            Assert.False((await new CreateComment(fake, fake, new CommentsService.Application.Comments.Markup.XhtmlCommentMarkup()).ExecuteAsync(input, Client)).IsValid);
            Assert.Equal(0, fake.SaveCalls);
        }
    }

    [Fact]
    public async Task MaximumTextLengthAndHttpsHomepageAreAccepted()
    {
        var fake = new Storage();
        var result = await new CreateComment(fake, fake, new CommentsService.Application.Comments.Markup.XhtmlCommentMarkup()).ExecuteAsync(Input with
            { Text = new string('a', 65536), HomePage = "https://example.test/path?q=1" }, Client);
        Assert.True(result.IsValid);
        Assert.Equal(65536, result.Value!.Text.Length);
        Assert.Equal("https://example.test/path?q=1", result.Value.HomePage);
    }

    [Fact]
    public async Task CancelledCreateDoesNotStageData()
    {
        var fake = new Storage();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new CreateComment(fake, fake, new CommentsService.Application.Comments.Markup.XhtmlCommentMarkup())
            .ExecuteAsync(Input, Client, new CancellationToken(true)));
        Assert.Empty(fake.Comments);
    }

    [Fact]
    public async Task FailedSaveCannotReturnSuccess()
    {
        var fake = new Storage { FailSave = true };
        await Assert.ThrowsAsync<StorageUnavailableException>(() => new CreateComment(fake, fake, new CommentsService.Application.Comments.Markup.XhtmlCommentMarkup()).ExecuteAsync(Input, Client));
        Assert.Equal(1, fake.SaveCalls);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    public async Task LatestListRejectsInvalidLimits(int limit)
    {
        var fake = new Storage();
        var result = await new GetLatestComments(fake).ExecuteAsync(limit);
        Assert.False(result.IsValid);
        Assert.Contains("limit", result.Errors.Keys);
        Assert.Equal(0, fake.QueryCalls);
    }

    [Fact]
    public async Task ReadUseCasesDelegateIdentifiersLimitsAndCancellation()
    {
        var fake = new Storage();
        var created = await new CreateComment(fake, fake, new CommentsService.Application.Comments.Markup.XhtmlCommentMarkup()).ExecuteAsync(Input, Client);
        var id = created.Value!.Id;
        using var cancellation = new CancellationTokenSource();
        Assert.Equal(created.Value, await new GetComment(fake).ExecuteAsync(id, cancellation.Token));
        Assert.Equal(cancellation.Token, fake.Token);
        Assert.Null(await new GetComment(fake).ExecuteAsync(Guid.NewGuid()));
        var latest = await new GetLatestComments(fake).ExecuteAsync(1, cancellation.Token);
        Assert.True(latest.IsValid);
        Assert.Equal(id, Assert.Single(latest.Value!).Id);
        Assert.Equal(1, fake.Limit);
        Assert.Equal(cancellation.Token, fake.Token);
    }

    private sealed class Storage : ICommentRepository, IUnitOfWork, ICommentQueries
    {
        public List<Comment> Comments { get; } = [];
        public int SaveCalls { get; private set; }
        public int QueryCalls { get; private set; }
        public int Limit { get; private set; }
        public bool FailSave { get; init; }
        public CancellationToken Token { get; private set; }
        public void Add(Comment comment) => Comments.Add(comment);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            Token = cancellationToken;
            if (FailSave) throw new StorageUnavailableException("Storage failed.");
            return Task.FromResult(Comments.Count * 2);
        }
        Task<Comment?> ICommentRepository.FindByIdAsync(Guid id, CancellationToken token) =>
            Task.FromResult(Comments.SingleOrDefault(x => x.Id == id));
        public Task<CommentDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            QueryCalls++; Token = cancellationToken;
            return Task.FromResult(Comments.SingleOrDefault(x => x.Id == id) is { } found ? CommentDto.From(found) : null);
        }
        public Task<IReadOnlyList<CommentDto>> GetRepliesAsync(Guid parentId, int limit,
            CommentsService.Application.Comments.GetReplies.ReplyCursor? after, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CommentDto>>(Comments.Where(c => c.ParentId == parentId).Select(CommentDto.From).Take(limit).ToArray());
        public Task<IReadOnlyList<CommentDto>> GetLatestAsync(int limit, CancellationToken cancellationToken = default)
        {
            QueryCalls++; Token = cancellationToken; Limit = limit;
            return Task.FromResult<IReadOnlyList<CommentDto>>(Comments.Take(limit).Select(CommentDto.From).ToArray());
        }
    }
}
