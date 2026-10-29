using Npgsql;

namespace CommentsService.Infrastructure.Persistence;

public static class DatabaseOptions
{
    // Standard libpq-style environment variables; never log the resulting connection string.
    public static string ConnectionString()
    {
        string Required(string name) => Environment.GetEnvironmentVariable(name)
            ?? throw new InvalidOperationException($"Set {name} before using database commands.");
        return new NpgsqlConnectionStringBuilder
        {
            Host = Required("PGHOST"),
            Port = int.Parse(Environment.GetEnvironmentVariable("PGPORT") ?? "5432"),
            Database = Required("PGDATABASE"),
            Username = Required("PGUSER"),
            Password = Required("PGPASSWORD"),
            IncludeErrorDetail = false
        }.ConnectionString;
    }
}
