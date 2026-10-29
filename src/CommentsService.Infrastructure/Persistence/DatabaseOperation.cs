using CommentsService.Application.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CommentsService.Infrastructure.Persistence;

internal static class DatabaseOperation
{
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try { return await operation(); }
        catch (Exception exception) when (Unavailable(exception))
        {
            throw new StorageUnavailableException("Comments storage is unavailable.", exception);
        }
    }

    private static bool Unavailable(Exception exception) => exception switch
    {
        PostgresException postgres => postgres.SqlState.StartsWith("08", StringComparison.Ordinal) ||
            postgres.SqlState is "57P01" or "57P02" or "57P03" or "53300",
        NpgsqlException or TimeoutException => true,
        DbUpdateException { InnerException: { } inner } => Unavailable(inner),
        InvalidOperationException { InnerException: { } inner } => Unavailable(inner),
        _ => false
    };
}
