namespace CommentsService.Api.Domain;

// A submitted identity, not an authenticated account. Email is deliberately not unique.
public sealed class Author
{
    private Author() { }

    public Author(string userName, string email, string? homePage = null)
    {
        UserName = userName;
        Email = email;
        HomePage = homePage;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UserName { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string? HomePage { get; private set; }
    public ICollection<Comment> Comments { get; private set; } = new List<Comment>();
}
