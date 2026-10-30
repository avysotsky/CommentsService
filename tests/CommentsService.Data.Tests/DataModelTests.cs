using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using CommentsService.Infrastructure.Persistence;
using CommentsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CommentsService.Data.Tests;

// Every test owns a transaction. Neither migrations nor database reset belong in fixture setup.
public sealed class DataModelTests : IAsyncLifetime
{
    private CommentsDbContext db = null!;
    private ServiceProvider provider = null!;
    private AsyncServiceScope scope;

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("PGDATABASE") != "comments_test")
            throw new InvalidOperationException("Integration tests require PGDATABASE=comments_test.");
        provider = new ServiceCollection().AddLogging().AddInfrastructure()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        scope = provider.CreateAsyncScope();
        db = scope.ServiceProvider.GetRequiredService<CommentsDbContext>();
        await db.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        await db.Database.RollbackTransactionAsync();
        await scope.DisposeAsync();
        await provider.DisposeAsync();
    }

    [Theory]
    [InlineData(0, false)] [InlineData(0, true)]
    [InlineData(1, false)] [InlineData(1, true)]
    [InlineData(2, false)] [InlineData(2, true)]
    public async Task RootPagesSortAllDirectionsExcludeRepliesAndDoNotLoseTiedRows(int sortValue, bool descending)
    {
        var rootsBefore = await db.Comments.CountAsync(x => x.ParentId == null);
        var own = new List<Comment>();
        for (var i = 0; i < 54; i++)
        {
            var author = new Author("Paging" + (i % 4), "paging" + (i % 3) + "@example.test");
            var root = new Comment(author, "page fixture", IPAddress.Loopback, ""); own.Add(root); db.Add(root);
            db.Entry(root).Property(x => x.CreatedAt).CurrentValue = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        }
        db.Add(new Comment(new Author("PagingReply", "reply@example.test"), "not a root", IPAddress.Loopback, "", own[0]));
        await db.SaveChangesAsync();
        var queries = scope.ServiceProvider.GetRequiredService<ICommentQueries>();
        var sort = (CommentsService.Application.Comments.GetRootPage.CommentSort)sortValue;
        var direction = descending ? CommentsService.Application.Comments.GetRootPage.SortDirection.Desc : CommentsService.Application.Comments.GetRootPage.SortDirection.Asc;
        var found = new List<Guid>(); long total = 0;
        for (var page = 1; ; page++)
        {
            var result = await queries.GetRootPageAsync(new(page, sort, direction)); total = result.TotalCount;
            Assert.Equal(25, result.PageSize); Assert.Equal(rootsBefore + 54, result.TotalCount);
            Assert.Equal((result.TotalCount + 24) / 25, result.TotalPages);
            Assert.All(result.Items, x => Assert.Null(x.ParentId));
            Assert.InRange(result.Items.Count, 1, 25);
            found.AddRange(result.Items.Select(x => x.Id));
            if (page == result.TotalPages) break;
        }
        Assert.Equal(total, found.Count); Assert.Equal(found.Count, found.Distinct().Count());
        Func<Comment, string> key = sortValue == 0 ? x => x.Author.UserName : sortValue == 1 ? x => x.Author.Email : _ => "same";
        var ordered = descending ? own.OrderByDescending(key, StringComparer.Ordinal).ThenByDescending(x => x.Id) : own.OrderBy(key, StringComparer.Ordinal).ThenBy(x => x.Id);
        Assert.Equal(ordered.Select(x => x.Id), found.Where(id => own.Any(x => x.Id == id)));
        var beyond = await queries.GetRootPageAsync(new((int)((total + 24) / 25 + 1), sort, direction));
        Assert.Empty(beyond.Items); Assert.Equal(total, beyond.TotalCount);
    }

    [Fact]
    public async Task ThreadLinksRoundTripAndRootsAreListedSeparately()
    {
        var root = new Comment(new Author("Root", "r@example.test"), "root", IPAddress.Loopback, "");
        var parent = root; db.Add(root);
        for (var depth = 0; depth < 40; depth++)
        {
            var reply = new Comment(new Author("Reply", "r@example.test"), "reply", IPAddress.Loopback, "", parent);
            db.Add(reply); parent = reply;
        }
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var saved = await db.Comments.SingleAsync(x => x.Id == parent.Id);
        Assert.Equal(root.Id, saved.RootId); Assert.NotNull(saved.ParentId);
        var queries = scope.ServiceProvider.GetRequiredService<ICommentQueries>();
        var roots = await queries.GetLatestAsync(100);
        Assert.Contains(roots, x => x.Id == root.Id); Assert.DoesNotContain(roots, x => x.Id == parent.Id);
        Assert.All(roots, x => Assert.Null(x.ParentId));
    }

    [Fact]
    public async Task DatabaseRejectsAReplyWithWrongRoot()
    {
        var root = new Comment(new Author("Root", "r@example.test"), "root", IPAddress.Loopback, "");
        var other = new Comment(new Author("Other", "r@example.test"), "root", IPAddress.Loopback, "");
        db.AddRange(root, other); await db.SaveChangesAsync();
        var child = new Comment(new Author("Child", "r@example.test"), "child", IPAddress.Loopback, "", root);
        db.Add(child); db.Entry(child).Property(x => x.RootId).CurrentValue = other.Id;
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }

    [Fact]
    public async Task DatabaseRestrictsDeletingAParentWithReplies()
    {
        var root = new Comment(new Author("Root", "r@example.test"), "root", IPAddress.Loopback, "");
        db.Add(root); db.Add(new Comment(new Author("Child", "r@example.test"), "child", IPAddress.Loopback, "", root));
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM comments.\"Comments\" WHERE \"Id\" = {root.Id}"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
    }

    [Fact]
    public async Task RepliesWithEqualTimesUseStableKeysetPages()
    {
        var root = new Comment(new Author("Root", "r@example.test"), "root", IPAddress.Loopback, "");
        db.Add(root); var ids = new HashSet<Guid>();
        for (var i = 0; i < 6; i++)
        {
            var child = new Comment(new Author("Child", "r@example.test"), "child", IPAddress.Loopback, "", root);
            db.Add(child); db.Entry(child).Property(x => x.CreatedAt).CurrentValue = DateTimeOffset.Parse("2026-01-01T00:00:00Z"); ids.Add(child.Id);
        }
        await db.SaveChangesAsync();
        var queries = scope.ServiceProvider.GetRequiredService<ICommentQueries>();
        CommentsService.Application.Comments.GetReplies.ReplyCursor? cursor = null;
        var found = new List<Guid>();
        for (var page = 0; page < 4; page++)
        {
            var rows = await queries.GetRepliesAsync(root.Id, 2, cursor);
            found.AddRange(rows.Select(x => x.Id));
            if (rows.Count == 0) break;
            cursor = new(rows[^1].CreatedAt, rows[^1].Id);
        }
        Assert.Equal(6, found.Distinct().Count()); Assert.Equal(6, found.Count); Assert.True(ids.SetEquals(found));
    }

    [Fact]
    public async Task RoundTripPreservesIdentityUnicodeClientAndUtc()
    {
        var author = new Author("Reader123", "reader@example.test", "https://example.test/");
        var comment = new Comment(author, "Hello / Привіт / Привет / مرحبا / 😀", IPAddress.IPv6Loopback, "DataModel test");
        db.Add(comment);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var saved = await db.Comments.Include(x => x.Author).SingleAsync(x => x.Id == comment.Id);
        Assert.Equal(comment.Text, saved.Text);
        Assert.Equal(author.Id, saved.AuthorId);
        Assert.Same(saved, Assert.Single(saved.Author.Comments));
        Assert.Equal(author.UserName, saved.Author.UserName);
        Assert.Equal(author.Email, saved.Author.Email);
        Assert.Equal(author.HomePage, saved.Author.HomePage);
        Assert.Equal(IPAddress.IPv6Loopback, saved.ClientIp);
        Assert.Equal("DataModel test", saved.UserAgent);
        Assert.Equal(TimeSpan.Zero, saved.CreatedAt.Offset);
        Assert.InRange((saved.CreatedAt - comment.CreatedAt).Duration(), TimeSpan.Zero, TimeSpan.FromMicroseconds(1));
    }

    [Fact]
    public async Task SameEmailDoesNotImplyAnAuthenticatedAccount()
    {
        var first = new Author("First", "same@example.test");
        var second = new Author("Second", "same@example.test");
        db.AddRange(new Comment(first, "One", IPAddress.Loopback, ""), new Comment(second, "Two", IPAddress.Loopback, ""));
        await db.SaveChangesAsync();
        Assert.NotEqual(first.Id, second.Id);
        Assert.Null(second.HomePage);
    }

    [Fact]
    public async Task DeletingAnAuthorCannotSilentlyDeleteComments()
    {
        var author = new Author("Reader", "r@example.test");
        db.Add(new Comment(author, "Keep me", IPAddress.Loopback, ""));
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM comments.\"Authors\" WHERE \"Id\" = {author.Id}"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
    }

    [Theory]
    [InlineData("Имя", "Valid text", PostgresErrorCodes.CheckViolation)]
    [InlineData("Reader", "   ", PostgresErrorCodes.CheckViolation)]
    public async Task DatabaseRejectsInvalidRequiredContent(string userName, string text, string sqlState)
    {
        var author = new Author("ValidName", "r@example.test");
        var comment = new Comment(author, "Valid text", IPAddress.Loopback, "");
        db.Add(comment);
        // Bypass constructors deliberately to test database constraints independently.
        db.Entry(author).Property(x => x.UserName).CurrentValue = userName;
        db.Entry(comment).Property(x => x.Text).CurrentValue = text;
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(sqlState, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }

    [Fact]
    public async Task DatabaseEnforcesFieldLengths()
    {
        var author = new Author("ValidName", "r@example.test");
        db.Add(new Comment(author, "text", IPAddress.Loopback, ""));
        db.Entry(author).Property(x => x.UserName).CurrentValue = new string('A', 101);
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }

    [Fact]
    public async Task MigrationHistoryAndModelAreCurrent()
    {
        Assert.Equal(2, (await db.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task ApplicationRoleHasNoElevatedPrivileges()
    {
        var elevated = await db.Database.SqlQueryRaw<bool>(
            "SELECT (rolsuper OR rolcreatedb OR rolcreaterole) AS \"Value\" FROM pg_roles WHERE rolname=current_user").SingleAsync();
        Assert.False(elevated);
    }

    [Fact]
    public async Task ApplicationPortsShareScopeAndPersistAuthorWithComment()
    {
        var repository = scope.ServiceProvider.GetRequiredService<ICommentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var author = new Author("PortReader", "port@example.test");
        var comment = new Comment(author, "Порт збереження 😀", IPAddress.IPv6Loopback, "Application port test");
        repository.Add(comment);
        Assert.False(await db.Comments.AsNoTracking().AnyAsync(x => x.Id == comment.Id));
        Assert.Equal(2, await unitOfWork.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var saved = await repository.FindByIdAsync(comment.Id);
        Assert.NotNull(saved);
        Assert.Equal(comment.Text, saved.Text);
        Assert.Equal(author.Id, saved.AuthorId);
        Assert.Equal(author.Email, saved.Author.Email);
        Assert.Same(saved, Assert.Single(saved.Author.Comments));
    }

    [Fact]
    public async Task RepositoryReturnsNullForMissingComment()
    {
        var repository = scope.ServiceProvider.GetRequiredService<ICommentRepository>();
        Assert.Null(await repository.FindByIdAsync(Guid.NewGuid()));
    }
}
