using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CommentsService.Api.Data;

public sealed class CommentsDbContextFactory : IDesignTimeDbContextFactory<CommentsDbContext>
{
    public CommentsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CommentsDbContext>()
            .UseNpgsql(DatabaseOptions.ConnectionString(),
                pg => pg.MigrationsHistoryTable("__EFMigrationsHistory", "comments"))
            .Options;
        return new CommentsDbContext(options);
    }
}
