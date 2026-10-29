using CommentsService.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CommentsService.Infrastructure.Persistence;

internal sealed class EfDatabaseStatusReader(CommentsDbContext db) : IDatabaseStatusReader
{
    public async Task<DatabaseStatus> ReadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await db.Database.CanConnectAsync(cancellationToken)) return new(false, []);
            var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
            var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
            return new(!pending.Any() && !db.Database.HasPendingModelChanges(), applied.ToArray());
        }
        catch (InvalidOperationException) { return new(false, []); }
        catch (NpgsqlException) { return new(false, []); }
    }
}
