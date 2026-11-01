using CommentsService.Application.Cloud;
using CommentsService.Domain.Entities;
using CommentsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace CommentsService.Infrastructure.Cloud;

internal sealed class CloudAttachmentSource(CommentsDbContext db, CloudSettings settings) : ICloudAttachmentSource
{
    public async Task<CloudAttachment?> FindByCommentAsync(Guid commentId, CancellationToken token = default)
    {
        if (!settings.Enabled || !settings.AllowedCommentIds.Contains(commentId)) return null;
        var image = await db.Set<ImageAttachment>().AsNoTracking().Where(x => x.CommentId == commentId)
            .Select(x => new CloudAttachment(x.Id, x.CommentId, AttachmentKind.Image, x.Data)).SingleOrDefaultAsync(token);
        return image ?? await db.Set<TextAttachment>().AsNoTracking().Where(x => x.CommentId == commentId)
            .Select(x => new CloudAttachment(x.Id, x.CommentId, AttachmentKind.Text, x.Data)).SingleOrDefaultAsync(token);
    }
}
