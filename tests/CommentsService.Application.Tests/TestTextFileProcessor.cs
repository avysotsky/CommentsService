using CommentsService.Application.TextFiles;
using CommentsService.Application.Common;
namespace CommentsService.Application.Tests;
internal sealed class TestTextFileProcessor : ITextFileProcessor
{
    public Task<Validated<PreparedTextFile>> ProcessAsync(TextFileUpload upload, CancellationToken cancellationToken = default) => throw new InvalidOperationException("This test does not upload TXT.");
}
