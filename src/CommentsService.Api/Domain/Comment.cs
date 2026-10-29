using System.Net;

namespace CommentsService.Api.Domain;

public sealed class Comment
{
    private Comment() { }

    public Comment(Author author, string text, IPAddress clientIp, string userAgent)
    {
        Author = author;
        AuthorId = author.Id;
        Text = text;
        ClientIp = clientIp;
        UserAgent = userAgent;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid AuthorId { get; private set; }
    public Author Author { get; private set; } = null!;
    public string Text { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public IPAddress ClientIp { get; private set; } = null!;
    public string UserAgent { get; private set; } = null!;
}
