using System.Net.Mail;
using System.Text.RegularExpressions;
using CommentsService.Domain.Entities;

namespace CommentsService.Application.Comments.CreateComment;

internal static class CreateCommentValidator
{
    private static readonly Regex TagStart = new(@"<\s*[/!?\p{L}]",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public static Dictionary<string, string[]> Validate(CreateCommentInput input)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(input.UserName) || input.UserName.Length > Author.UserNameMaxLength ||
            input.UserName.Any(c => !char.IsAsciiLetterOrDigit(c)))
            errors["userName"] = ["User name must contain 1–100 Latin letters or digits."];
        if (string.IsNullOrWhiteSpace(input.Email) || input.Email.Length > Author.EmailMaxLength ||
            input.Email.Any(char.IsControl) || !MailAddress.TryCreate(input.Email, out var address) ||
            !string.Equals(address.Address, input.Email, StringComparison.Ordinal))
            errors["email"] = ["Enter a valid email address, up to 254 characters, without a display name."];
        if (input.HomePage is { } url && (url.Length > Author.HomePageMaxLength || url.Any(char.IsControl) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo) ||
            !Uri.IsWellFormedUriString(url, UriKind.Absolute)))
            errors["homePage"] = ["Homepage must be an absolute HTTP or HTTPS URL, up to 2048 characters, without credentials."];
        if (string.IsNullOrWhiteSpace(input.Text) || input.Text.Length > Comment.TextMaxLength)
            errors["text"] = ["Comment text is required and must not exceed 65536 characters."];
        else if (!ValidUnicode(input.Text) || input.Text.Contains('\0'))
            errors["text"] = ["Comment text must contain valid Unicode and no null characters."];
        else if (TagStart.IsMatch(input.Text))
            errors["text"] = ["HTML markup is not supported in this release. Enter plain text."];
        return errors;
    }

    private static bool ValidUnicode(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (!char.IsSurrogate(value[i])) continue;
            if (!char.IsHighSurrogate(value[i]) || i + 1 == value.Length || !char.IsLowSurrogate(value[++i])) return false;
        }
        return true;
    }
}
