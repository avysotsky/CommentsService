using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Common;
using CommentsService.Domain.Entities;

namespace CommentsService.Application.Comments.CreateComment;

public sealed class CreateComment(ICommentRepository repository, IUnitOfWork unitOfWork, CommentsService.Application.Comments.Markup.ICommentMarkup markup, CommentsService.Application.Captcha.ICaptchaVerifier captcha, CommentsService.Application.Images.IImageProcessor images, CommentsService.Application.TextFiles.ITextFileProcessor textFiles) : ICreateComment
{
    public async Task<Validated<CommentDto>> ExecuteAsync(CreateCommentInput input, ClientMetadata client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(client);
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = input with { UserName = input.UserName?.Trim(), Email = input.Email?.Trim(),
            HomePage = string.IsNullOrWhiteSpace(input.HomePage) ? null : input.HomePage.Trim() };
        var errors = CreateCommentValidator.Validate(normalized, markup);
        if (normalized.Image is not null && normalized.TextFile is not null) errors["textFile"] = ["Choose either an image or a TXT file."];
        if (errors.Count != 0) return Validated<CommentDto>.Invalid(errors);
        if (!captcha.Verify(normalized.CaptchaId, normalized.CaptchaCode))
            return Validated<CommentDto>.Invalid(new() { ["captchaCode"] = ["CAPTCHA is invalid, expired or already used. Refresh it and enter the new code."] });
        Comment? parent = null;
        if (normalized.ParentId is { } parentId)
        {
            parent = await repository.FindByIdAsync(parentId, cancellationToken);
            if (parent is null) return Validated<CommentDto>.Invalid(new() { ["parentId"] = ["Reply target was not found. Choose an existing comment."] });
        }
        CommentsService.Application.Images.PreparedImage? image = null;
        if (normalized.Image is not null)
        {
            var processed = await images.ProcessAsync(normalized.Image, cancellationToken);
            if (!processed.IsValid) return Validated<CommentDto>.Invalid(processed.Errors.ToDictionary());
            image = processed.Value;
        }
        CommentsService.Application.TextFiles.PreparedTextFile? textFile = null;
        if (normalized.TextFile is not null)
        {
            var processed = await textFiles.ProcessAsync(normalized.TextFile, cancellationToken);
            if (!processed.IsValid) return Validated<CommentDto>.Invalid(processed.Errors.ToDictionary());
            textFile = processed.Value;
        }
        var author = new Author(normalized.UserName!, normalized.Email!, normalized.HomePage);
        var comment = new Comment(author, normalized.Text!, client.IpAddress, client.UserAgent, parent);
        if (image is not null) _ = new ImageAttachment(comment, image.FileName, image.Data, image.Width, image.Height);
        if (textFile is not null) _ = new TextAttachment(comment, textFile.FileName, textFile.Data);
        repository.Add(comment);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Validated<CommentDto>.Success(CommentMapping.From(comment));
    }
}
