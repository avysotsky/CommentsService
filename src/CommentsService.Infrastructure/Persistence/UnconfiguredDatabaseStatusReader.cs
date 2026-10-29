using CommentsService.Infrastructure.Abstractions;

namespace CommentsService.Infrastructure.Persistence;

internal sealed class UnconfiguredDatabaseStatusReader : IDatabaseStatusReader
{
    public Task<DatabaseStatus> ReadAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new DatabaseStatus(false, []));
}
