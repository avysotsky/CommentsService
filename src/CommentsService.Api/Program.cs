using CommentsService.Application.Comments.CreateComment;
using CommentsService.Application.Comments.GetComment;
using CommentsService.Application.Comments.GetLatestComments;
using CommentsService.Api.Errors;
using CommentsService.Infrastructure;
using CommentsService.Infrastructure.Abstractions;
using CommentsService.Api.Endpoints;

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
builder.Services.AddSingleton<CommentsService.Application.Comments.Markup.ICommentMarkup, CommentsService.Application.Comments.Markup.XhtmlCommentMarkup>();
builder.Services.AddScoped<ICreateComment, CreateComment>();
builder.Services.AddScoped<IGetComment, GetComment>();
builder.Services.AddScoped<CommentsService.Application.Comments.GetRootPage.IGetRootPage, CommentsService.Application.Comments.GetRootPage.GetRootPage>();
builder.Services.AddScoped<CommentsService.Application.Comments.GetReplies.IGetReplies, CommentsService.Application.Comments.GetReplies.GetReplies>();
builder.Services.AddScoped<IGetLatestComments, GetLatestComments>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1024 * 1024);
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

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapSystemEndpoints();
app.MapCommentsEndpoints();
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("/{*path:nonfile}", "index.html");

app.Run();
return 0;

public partial class Program { }
