using Npgsql;
using CommentsService.Application.Common;

namespace CommentsService.Infrastructure.Persistence;

public static class DatabaseOptions
{
    // Standard libpq-style environment variables; never log the resulting connection string.
    public static string ConnectionString(DatabaseSettings? settings = null)
    {
        var supplied = settings is not null;
        settings ??= new();
        string Required(string name, string? fallback = null) => fallback ?? Environment.GetEnvironmentVariable(name)
            ?? throw new StorageUnavailableException($"Set {name} before using database commands.");
        return new NpgsqlConnectionStringBuilder
        {
            Host = Required("PGHOST", settings.Host),
            Port = supplied ? settings.Port : int.Parse(Environment.GetEnvironmentVariable("PGPORT") ?? "5432"),
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
