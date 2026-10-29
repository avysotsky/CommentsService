using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/status", () => new ApplicationStatus(
    "CommentsService",
    Assembly.GetExecutingAssembly().GetName().Version!.ToString(),
    "Init"));

app.Run();

public sealed record ApplicationStatus(string Application, string Version, string Stage);
