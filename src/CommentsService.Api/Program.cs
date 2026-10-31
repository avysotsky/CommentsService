using CommentsService.Api.Security;
using CommentsService.Api.Configuration;
using CommentsService.Api.Logging;
using Serilog.Context;
using System.Diagnostics;
using CommentsService.Application.Comments.CreateComment;
using CommentsService.Application.Comments.GetComment;
using CommentsService.Application.Comments.GetLatestComments;
using CommentsService.Api.Errors;
using CommentsService.Infrastructure;
using CommentsService.Infrastructure.Abstractions;
using CommentsService.Api.Endpoints;

var healthCommand = args.FirstOrDefault() == "--healthcheck";
var databaseCommand = args.FirstOrDefault() is "--migrate" or "--check-database";
var configurationArgs = databaseCommand || healthCommand ? args.Skip(1).ToArray() : args;
var builder = WebApplication.CreateBuilder(configurationArgs);
var runtime = RuntimeConfiguration.Load(builder.Configuration, configurationArgs);
if (healthCommand)
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
    try
    {
        using var response = await client.GetAsync($"http://127.0.0.1:{runtime.HealthPort}/health");
        return response.IsSuccessStatusCode ? 0 : 1;
    }
    catch (HttpRequestException) { return 1; }
    catch (TaskCanceledException) { return 1; }
}

builder.Services.AddSingleton(runtime);
RuntimeLogging.Configure(builder, runtime);
builder.Services.AddInfrastructure(runtime.Database);
builder.Services.AddSingleton<CommentsService.Application.Comments.Markup.ICommentMarkup, CommentsService.Application.Comments.Markup.XhtmlCommentMarkup>();
builder.Services.AddScoped<ICreateComment, CreateComment>();
builder.Services.AddScoped<IGetComment, GetComment>();
builder.Services.AddScoped<CommentsService.Application.TextFiles.IGetTextFile, CommentsService.Application.TextFiles.GetTextFile>();
builder.Services.AddScoped<CommentsService.Application.Images.IGetImage, CommentsService.Application.Images.GetImage>();
builder.Services.AddScoped<CommentsService.Application.Comments.GetRootPage.IGetRootPage, CommentsService.Application.Comments.GetRootPage.GetRootPage>();
builder.Services.AddScoped<CommentsService.Application.Comments.GetReplies.IGetReplies, CommentsService.Application.Comments.GetReplies.GetReplies>();
builder.Services.AddScoped<IGetLatestComments, GetLatestComments>();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5 * 1024 * 1024;
    options.ValueLengthLimit = 262144;
    options.ValueCountLimit = 16;
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.WebHost.ConfigureKestrel(options => { options.AddServerHeader = false; options.Limits.MaxRequestBodySize = 6 * 1024 * 1024; });
await using var app = builder.Build();
var operationLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("CommentsService.Api.Operations");
operationLogger.LogInformation("Host initialized for {Operation}", databaseCommand ? args[0] : "serve");
app.Lifetime.ApplicationStopping.Register(() => operationLogger.LogInformation("Host stopping"));

if (databaseCommand)
{
    await using var scope = app.Services.CreateAsyncScope();
    if (args[0] == "--migrate")
        await scope.ServiceProvider.GetRequiredService<IDatabaseMigrator>().MigrateAsync();
    var status = await scope.ServiceProvider.GetRequiredService<IDatabaseStatusReader>().ReadAsync();
    if (!status.IsReady)
    {
        operationLogger.LogWarning("Database command {Operation} completed with {Status}", args[0], "unavailable");
        return 1;
    }
    operationLogger.LogInformation("Database command {Operation} completed with {Status}", args[0], "ready");
    Console.WriteLine("Database schema is current: " + string.Join(", ", status.AppliedMigrations));
    return 0;
}

app.Use(async (context, next) =>
{
    using var trace = LogContext.PushProperty("TraceId", context.TraceIdentifier);
    var started = Stopwatch.GetTimestamp();
    await next(context);
    operationLogger.LogInformation("HTTP {Method} {Route} completed with {Status} in {ElapsedMs} ms",
        context.Request.Method, (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "static",
        context.Response.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
});
app.UseSecurityHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapSystemEndpoints();
app.MapCommentsEndpoints();
app.MapCaptchaEndpoints();
app.MapImageEndpoints();
app.MapTextFileEndpoints();
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("/{*path:nonfile}", "index.html");

app.Run();
return 0;

public partial class Program { }
