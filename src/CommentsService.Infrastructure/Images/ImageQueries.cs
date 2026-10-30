using CommentsService.Application.Images;
using CommentsService.Domain.Entities;
using CommentsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace CommentsService.Infrastructure.Images;

internal sealed class ImageQueries(CommentsDbContext db) : IImageQueries
{
    public Task<StoredImage?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        DatabaseOperation.ExecuteAsync(() => db.Set<ImageAttachment>().AsNoTracking().Where(x => x.Id == id)
            .Select(x => new StoredImage(x.Data)).SingleOrDefaultAsync(cancellationToken));
}
