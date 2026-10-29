using System.Net;
using CommentsService.Api.Data;
using CommentsService.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace CommentsService.Data.Tests;

// Every test owns a transaction. Neither migrations nor database reset belong in fixture setup.
public sealed class DataModelTests : IAsyncLifetime
{
    private CommentsDbContext db = null!;

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("PGDATABASE") != "comments_test")
            throw new InvalidOperationException("Integration tests require PGDATABASE=comments_test.");
        db = new CommentsDbContextFactory().CreateDbContext([]);
        await db.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        await db.Database.RollbackTransactionAsync();
        await db.DisposeAsync();
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
        db.Add(new Comment(new Author(userName, "r@example.test"), text, IPAddress.Loopback, ""));
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(sqlState, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }

    [Fact]
    public async Task DatabaseEnforcesFieldLengths()
    {
        db.Add(new Comment(new Author(new string('A', 101), "r@example.test"), "text", IPAddress.Loopback, ""));
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.StringDataRightTruncation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }

    [Fact]
    public async Task MigrationHistoryAndModelAreCurrent()
    {
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
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
}
