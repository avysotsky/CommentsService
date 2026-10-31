using System.Net.Mail;
using CommentsService.Application.Comments.Markup;
using CommentsService.Domain.Entities;

namespace CommentsService.Application.Comments.CreateComment;

internal static class CreateCommentValidator
{
    public static Dictionary<string, string[]> Validate(CreateCommentInput input, ICommentMarkup markup, CommentsService.Application.Common.ApplicationLimits limits)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.UserName) || input.UserName.Length > limits.UserNameMaxLength ||
            input.UserName.Any(c => !char.IsAsciiLetterOrDigit(c)))
            errors["userName"] = [$"User name must contain 1–{limits.UserNameMaxLength} Latin letters or digits."];
        if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > limits.EmailMaxLength ||
            input.Email.Any(char.IsControl) || !MailAddress.TryCreate(input.Email, out var address) ||
            !string.Equals(address.Address, input.Email, StringComparison.Ordinal))
            errors["email"] = [$"Enter a valid email address, up to {limits.EmailMaxLength} characters, without a display name."];
        if (input.HomePage is { } url && (url.Length > limits.HomePageMaxLength || url.Any(char.IsControl) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) ||
            !Uri.IsWellFormedUriString(url, UriKind.Absolute)))
            errors["homePage"] = [$"Homepage must be an absolute HTTP or HTTPS URL, up to {limits.HomePageMaxLength} characters, without credentials."];
        var body = markup.Validate(input.Text);
        if (!body.IsValid) errors["text"] = body.Errors["text"];
        return errors;
    }
}
