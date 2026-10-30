using CommentsService.Application.TextFiles;
using CommentsService.Domain.Entities;
using CommentsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace CommentsService.Infrastructure.TextFiles;
internal sealed class TextFileQueries(CommentsDbContext db) : ITextFileQueries
{
    public Task<StoredTextFile?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        DatabaseOperation.ExecuteAsync(() => db.Set<TextAttachment>().AsNoTracking().Where(x => x.Id == id)
            .Select(x => new StoredTextFile(x.Data)).SingleOrDefaultAsync(cancellationToken));
}
