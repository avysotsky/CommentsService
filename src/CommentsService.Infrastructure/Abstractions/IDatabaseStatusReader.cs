namespace CommentsService.Infrastructure.Abstractions;

public sealed record DatabaseStatus(bool IsReady, IReadOnlyList<string> AppliedMigrations);

public interface IDatabaseStatusReader
{
    Task<DatabaseStatus> ReadAsync(CancellationToken cancellationToken = default);
}
