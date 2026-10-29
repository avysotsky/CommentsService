using CommentsService.Infrastructure;
using CommentsService.Infrastructure.Abstractions;
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

var databaseCommand = args is ["--migrate"] or ["--check-database"];
var builder = WebApplication.CreateBuilder(databaseCommand ? [] : args);
builder.Services.AddInfrastructure();
await using var app = builder.Build();

if (databaseCommand)
{
    await using var scope = app.Services.CreateAsyncScope();
    if (args[0] == "--migrate")
        await scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>().MigrateAsync();
    var status = await scope.ServiceProvider.GetRequiredService<IDatabaseStatusReader>().ReadAsync();
    if (!status.IsReady) return 1;
    Console.WriteLine("Database schema is current: " + string.Join(", ", status.AppliedMigrations));
    return 0;
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", () => new ApplicationStatus(
    "CommentsService",
    Assembly.GetExecutingAssembly().GetName().Version!.ToString(),
    "DataModel"));

// Process readiness only: database health is checked separately by Compose.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/database", async (IDatabaseStatusReader database, CancellationToken cancellationToken) =>
    (await database.ReadAsync(cancellationToken)).IsReady
        ? Results.Ok(new { status = "ok" })
        : Results.StatusCode(503));
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("/{*path:nonfile}", "index.html");

app.Run();
return 0;

public sealed record ApplicationStatus(string Application, string Version, string Stage);
