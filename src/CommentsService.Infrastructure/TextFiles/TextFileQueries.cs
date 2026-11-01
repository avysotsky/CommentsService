using CommentsService.Application.TextFiles;
using CommentsService.Application.Cloud;
using CommentsService.Domain.Entities;
using CommentsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace CommentsService.Infrastructure.TextFiles;

internal sealed class TextFileQueries(CommentsDbContext db, IAttachmentCloud? cloud = null) : ITextFileQueries
{
    public async Task<StoredTextFile?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var original = await DatabaseOperation.ExecuteAsync(() => db.Set<TextAttachment>().AsNoTracking().Where(x => x.Id == id)
            .Select(x => new CloudAttachment(x.Id, x.CommentId, AttachmentKind.Text, x.Data)).SingleOrDefaultAsync(cancellationToken));
        if (original is null) return null;
        var bytes = cloud is null ? original.Data : await cloud.ReadOrOriginalAsync(original, cancellationToken);
        return new(bytes);
    }
}
