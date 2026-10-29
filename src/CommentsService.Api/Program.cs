using CommentsService.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

if (args is ["--healthcheck"])
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
    try
    {
        using var response = await client.GetAsync("http://127.0.0.1:8080/health");
        return response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException) { return 1; }
    catch (TaskCanceledException) { return 1; }
}

if (args is ["--migrate"] or ["--check-database"])
{
    await using var db = new CommentsDbContextFactory().CreateDbContext([]);
    if (args[0] == "--migrate") await db.Database.MigrateAsync();
    else
    {
        if (!await db.Database.CanConnectAsync()) return 1;
        if ((await db.Database.GetPendingMigrationsAsync()).Any()) return 1;
        if (db.Database.HasPendingModelChanges()) return 1;
    }
    Console.WriteLine("Database schema is current: " +
        string.Join(", ", await db.Database.GetAppliedMigrationsAsync()));
    return 0;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CommentsDbContext>(options =>
        options.UseNpgsql(DatabaseOptions.ConnectionString(),
            pg => pg.MigrationsHistoryTable("__EFMigrationsHistory", "comments")));
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", () => new ApplicationStatus(
    "CommentsService",
    Assembly.GetExecutingAssembly().GetName().Version!.ToString(),
    "DataModel"));

// Process readiness only: database health is checked separately by Compose.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/database", async (IServiceProvider services) =>
{
    try
    {
        var db = services.GetRequiredService<CommentsDbContext>();
        if (!await db.Database.CanConnectAsync()) return Results.StatusCode(503);
        return (await db.Database.GetPendingMigrationsAsync()).Any()
            ? Results.StatusCode(503)
            : Results.Ok(new { status = "ok" });
    }
    catch (InvalidOperationException) { return Results.StatusCode(503); }
    catch (Npgsql.NpgsqlException) { return Results.StatusCode(503); }
});
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("/{*path:nonfile}", "index.html");

app.Run();
return 0;

public sealed record ApplicationStatus(string Application, string Version, string Stage);
