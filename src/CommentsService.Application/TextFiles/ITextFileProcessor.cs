using CommentsService.Application.Common;
namespace CommentsService.Application.TextFiles;

public interface ITextFileProcessor
{
    Task<Validated<PreparedTextFile>> ProcessAsync(TextFileUpload upload, CancellationToken cancellationToken = default);
}
public interface ITextFileQueries { Task<StoredTextFile?> FindAsync(Guid id, CancellationToken cancellationToken = default); }
public interface IGetTextFile { Task<StoredTextFile?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default); }
public sealed class GetTextFile(ITextFileQueries queries) : IGetTextFile
{
    public Task<StoredTextFile?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) => queries.FindAsync(id, cancellationToken);
}
