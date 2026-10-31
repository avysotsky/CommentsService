using CommentsService.Application.Common;
namespace CommentsService.Application.Comments.Markup;

public interface ICommentMarkup
{
    Validated<CommentPreview> Validate(string? text);
}
