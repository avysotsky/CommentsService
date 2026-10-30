using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CommentsService.Application.Common;
using CommentsService.Domain.Entities;

namespace CommentsService.Application.Comments.Markup;

// A bounded, strict fragment parser. Never repair or silently strip rejected markup.
public sealed class XhtmlCommentMarkup : ICommentMarkup
{
    private static readonly Regex MarkupStart = new(@"<\s*[/!?\p{L}]",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    public Validated<CommentPreview> Validate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > Comment.TextMaxLength)
            return Invalid("Comment text is required and must not exceed 65536 characters.");
        try { XmlConvert.VerifyXmlChars(text); }
        catch (XmlException) { return Invalid("Comment text must contain valid XML Unicode characters."); }
        if (!MarkupStart.IsMatch(text)) return Validated<CommentPreview>.Success(new(WebUtility.HtmlEncode(text)));
        try
        {
            using var reader = XmlReader.Create(new StringReader("<root>" + text + "</root>"), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
                MaxCharactersInDocument = Comment.TextMaxLength + 13, IgnoreWhitespace = false
            });
            var html = new StringBuilder();
            var links = 0; var nodes = 0; var visibleText = false;
            while (reader.Read())
            {
                if (++nodes > 4096 || reader.Depth > 32) return Invalid("Markup is too complex (maximum depth 32 and 4096 nodes).");
                if (reader.Depth == 0 && reader.Name == "root") continue;
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        var tag = reader.Name;
                        if (reader.NamespaceURI.Length != 0 || tag is not ("a" or "i" or "strong" or "code"))
                            return Invalid("Only a, i, strong and code tags are allowed.");
                        if (tag == "a" && links != 0) return Invalid("Links cannot be nested.");
                        string? href = null; string? title = null;
                        while (reader.MoveToNextAttribute())
                        {
                            if (tag != "a" || reader.NamespaceURI.Length != 0) return Invalid("Unsupported tag attribute.");
                            if (reader.Name == "href") href = reader.Value;
                            else if (reader.Name == "title") title = reader.Value;
                            else return Invalid("Only href and title are allowed on links.");
                        }
                        reader.MoveToElement();
                        html.Append('<').Append(tag);
                        if (tag == "a")
                        {
                            if (!SafeLink(href)) return Invalid("Links require an absolute HTTP(S) href without credentials or control characters.");
                            html.Append(" href=\"").Append(WebUtility.HtmlEncode(href)).Append("\" rel=\"noopener noreferrer nofollow\"");
                            if (title is not null) html.Append(" title=\"").Append(WebUtility.HtmlEncode(title)).Append('"');
                            if (!reader.IsEmptyElement) links++;
                        }
                        html.Append('>');
                        if (reader.IsEmptyElement) html.Append("</").Append(tag).Append('>');
                        break;
                    case XmlNodeType.EndElement:
                        html.Append("</").Append(reader.Name).Append('>');
                        if (reader.Name == "a") links--;
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                        visibleText |= !string.IsNullOrWhiteSpace(reader.Value);
                        html.Append(WebUtility.HtmlEncode(reader.Value));
                        break;
                    default: return Invalid("Comments, declarations and CDATA are not allowed.");
                }
            }
            return visibleText ? Validated<CommentPreview>.Success(new(html.ToString())) : Invalid("Comment must contain visible text.");
        }
        catch (XmlException) { return Invalid("Use well-formed XHTML with closed, correctly nested tags and escaped & and < characters."); }
    }

    private static bool SafeLink(string? value) => value is not null && value.Length <= 2048 &&
        !value.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)) &&
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme is "http" or "https" && uri.Host.Length != 0 && uri.UserInfo.Length == 0 &&
        Uri.IsWellFormedUriString(value, UriKind.Absolute);

    private static Validated<CommentPreview> Invalid(string message) =>
        Validated<CommentPreview>.Invalid(new() { ["text"] = [message] });
}
