using CommentsService.Infrastructure.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CommentsService.Infrastructure.Health;

internal sealed class DatabaseHealthCheck(IDatabaseStatusReader database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default) =>
        (await database.ReadAsync(cancellationToken)).IsReady
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Database is unavailable or schema is not current.");
}
