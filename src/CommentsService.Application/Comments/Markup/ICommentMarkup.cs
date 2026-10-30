using CommentsService.Application.Common;
namespace CommentsService.Application.Comments.Markup;

public sealed record CommentPreview(string Html);
public interface ICommentMarkup
{
    Validated<CommentPreview> Validate(string? text);
}
