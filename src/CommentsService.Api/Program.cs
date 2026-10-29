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

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", () => new ApplicationStatus(
    "CommentsService",
    Assembly.GetExecutingAssembly().GetName().Version!.ToString(),
    "Docker"));

// Process readiness only: database health is checked separately by Compose.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("/{*path:nonfile}", "index.html");

app.Run();
return 0;

public sealed record ApplicationStatus(string Application, string Version, string Stage);
