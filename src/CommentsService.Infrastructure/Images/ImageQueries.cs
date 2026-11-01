using CommentsService.Application.Images;
using CommentsService.Application.Cloud;
using CommentsService.Domain.Entities;
using CommentsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace CommentsService.Infrastructure.Images;

internal sealed class ImageQueries(CommentsDbContext db, IAttachmentCloud? cloud = null) : IImageQueries
{
    public async Task<StoredImage?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var original = await DatabaseOperation.ExecuteAsync(() => db.Set<ImageAttachment>().AsNoTracking().Where(x => x.Id == id)
            .Select(x => new CloudAttachment(x.Id, x.CommentId, AttachmentKind.Image, x.Data)).SingleOrDefaultAsync(cancellationToken));
        if (original is null) return null;
        var bytes = cloud is null ? original.Data : await cloud.ReadOrOriginalAsync(original, cancellationToken);
        return new(bytes);
    }
}
