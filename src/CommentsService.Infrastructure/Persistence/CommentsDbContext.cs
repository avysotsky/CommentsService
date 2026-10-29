using CommentsService.Domain.Entities;
using CommentsService.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CommentsService.Infrastructure.Persistence;

public sealed class CommentsDbContext(DbContextOptions<CommentsDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("comments");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommentsDbContext).Assembly);
    }
}
