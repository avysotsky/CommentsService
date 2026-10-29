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

    [Fact]
    public async Task ApplicationPortsShareScopeAndPersistAuthorWithComment()
    {
        var repository = scope.ServiceProvider.GetRequiredService<ICommentRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        Assert.Same(db, unitOfWork);
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
