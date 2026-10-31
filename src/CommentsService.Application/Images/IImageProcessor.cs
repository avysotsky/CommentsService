using CommentsService.Application.Common;
namespace CommentsService.Application.Images;

public interface IImageProcessor
{
    Task<Validated<PreparedImage>> ProcessAsync(ImageUpload upload, CancellationToken cancellationToken = default);
}
public interface IImageQueries { Task<StoredImage?> FindAsync(Guid id, CancellationToken cancellationToken = default); }
public interface IGetImage { Task<StoredImage?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default); }
public sealed class GetImage(IImageQueries queries) : IGetImage
{
    public Task<StoredImage?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) => queries.FindAsync(id, cancellationToken);
}
