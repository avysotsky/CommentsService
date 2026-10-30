using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CommentsService.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/status", () => new ApplicationStatus(
            "CommentsService", typeof(SystemEndpoints).Assembly.GetName().Version!.ToString(), "SortingPagination"));
        endpoints.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        endpoints.MapHealthChecks("/health/database", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("database"),
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new
            {
                status = report.Status == HealthStatus.Healthy ? "ok" : "unavailable"
            }, context.RequestAborted)
        });
        return endpoints;
    }

    private sealed record ApplicationStatus(string Application, string Version, string Stage);
}
