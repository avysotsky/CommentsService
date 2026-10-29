using System.Net;

namespace CommentsService.Domain.Entities;

public sealed class Comment
{
    public const int TextMaxLength = 65536;
    public const int UserAgentMaxLength = 2048;

    private Comment() { }

    public Comment(Author author, string text, IPAddress clientIp, string userAgent)
    {
        ArgumentNullException.ThrowIfNull(author);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(clientIp);
        ArgumentNullException.ThrowIfNull(userAgent);
        if (text.Length > TextMaxLength)
            throw new ArgumentException("Comment text is too long.", nameof(text));
        if (userAgent.Length > UserAgentMaxLength)
            throw new ArgumentException("User agent is too long.", nameof(userAgent));
        Author = author;
        AuthorId = author.Id;
        Text = text;
        ClientIp = clientIp;
        UserAgent = userAgent;
        author.AttachComment(this);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AuthorId { get; private set; }
    public Author Author { get; private set; } = null!;
    public string Text { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public IPAddress ClientIp { get; private set; } = null!;
    public string UserAgent { get; private set; } = null!;
}
