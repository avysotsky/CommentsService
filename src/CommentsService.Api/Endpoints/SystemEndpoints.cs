using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using CommentsService.Api.DTO.Responses;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CommentsService.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/status", () => new ApplicationStatus(
            "CommentsService", typeof(SystemEndpoints).Assembly.GetName().Version!.ToString(), "CloudAttachments"));
        endpoints.MapGet("/api/config", (CommentsService.Api.Configuration.RuntimeSettings runtime) =>
        {
            var limits = runtime.Limits;
            return new PublicConfiguration(limits.RootPageSize, limits.DefaultListLimit, limits.MaximumListLimit,
                limits.UserNameMaxLength, limits.EmailMaxLength, limits.HomePageMaxLength, limits.TextMaxLength,
                limits.ImageUploadMaxBytes, limits.ImageMaxWidth, limits.ImageMaxHeight, limits.TextFileMaxBytes,
                runtime.Captcha.CodeLength, runtime.Captcha.Alphabet, runtime.Captcha.LifetimeSeconds,
                runtime.Search.Enabled, runtime.Search.MaximumQueryLength, runtime.GraphQL.Enabled, runtime.Realtime.Enabled, runtime.Realtime.ReconnectDelayMilliseconds, runtime.Realtime.MaximumReconnectDelayMilliseconds);
        });
        endpoints.MapGet("/health", () => Results.Ok(new HealthResponse("ok")));
        endpoints.MapHealthChecks("/health/database", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("database"),
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new HealthResponse(
                report.Status == HealthStatus.Healthy ? "ok" : "unavailable"), context.RequestAborted)
        });
        return endpoints;
    }

}
