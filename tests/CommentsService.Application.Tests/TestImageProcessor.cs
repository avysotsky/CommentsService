using CommentsService.Application.Images;
using CommentsService.Application.Common;
namespace CommentsService.Application.Tests;
internal sealed class TestImageProcessor : IImageProcessor
{
    public Task<Validated<PreparedImage>> ProcessAsync(ImageUpload upload, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("This test does not upload an image.");
}
