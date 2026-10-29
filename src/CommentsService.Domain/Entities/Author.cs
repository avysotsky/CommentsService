namespace CommentsService.Domain.Entities;

// A submitted identity, not an authenticated account. Email is deliberately not unique.
public sealed class Author
{
    public const int UserNameMaxLength = 100;
    public const int EmailMaxLength = 254;
    public const int HomePageMaxLength = 2048;
    private readonly List<Comment> _comments = [];

    private Author() { }

    public Author(string userName, string email, string? homePage = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        if (userName.Length > UserNameMaxLength || userName.Any(c => !char.IsAsciiLetterOrDigit(c)))
            throw new ArgumentException("User name must contain 1–100 Latin letters or digits.", nameof(userName));
        if (email.Length > EmailMaxLength)
            throw new ArgumentException("Email is too long.", nameof(email));
        if (homePage?.Length > HomePageMaxLength)
            throw new ArgumentException("Homepage is too long.", nameof(homePage));
        UserName = userName;
        Email = email;
        HomePage = homePage;
    }

    internal void AttachComment(Comment comment) => _comments.Add(comment);

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? HomePage { get; private set; }
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
}
