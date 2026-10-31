using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using CommentsService.Api.DTO.Responses;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CommentsService.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/status", () => new ApplicationStatus(
            "CommentsService", typeof(SystemEndpoints).Assembly.GetName().Version!.ToString(), "DtoContracts"));
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
