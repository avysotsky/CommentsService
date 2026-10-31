using Npgsql;
using CommentsService.Application.Common;

namespace CommentsService.Infrastructure.Persistence;

public static class DatabaseOptions
{
    // Standard libpq-style environment variables; never log the resulting connection string.
    public static string ConnectionString(DatabaseSettings? settings = null)
    {
        settings ??= new();
        string Required(string name, string? fallback = null) => Environment.GetEnvironmentVariable(name) ?? fallback
            ?? throw new StorageUnavailableException($"Set {name} before using database commands.");
        return new NpgsqlConnectionStringBuilder
        {
            Host = Required("PGHOST", settings.Host),
            Port = int.Parse(Environment.GetEnvironmentVariable("PGPORT") ?? settings.Port.ToString()),
            Database = Required("PGDATABASE", settings.Database),
            Username = Required("PGUSER", settings.Username),
            Password = Required("PGPASSWORD"),
            Timeout = settings.TimeoutSeconds,
            CommandTimeout = settings.CommandTimeoutSeconds,
            MaxPoolSize = settings.MaximumPoolSize,
            IncludeErrorDetail = false
        }.ConnectionString;
    }
}
