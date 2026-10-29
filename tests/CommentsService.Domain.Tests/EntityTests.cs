using System.Net;
using CommentsService.Domain.Entities;
using Xunit;

namespace CommentsService.Domain.Tests;

public sealed class EntityTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Ім’я")]
    [InlineData("name-with-symbol")]
    public void AuthorRejectsInvalidNames(string? name) =>
        Assert.ThrowsAny<ArgumentException>(() => new Author(name!, "r@example.test"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AuthorRequiresEmail(string? email) =>
        Assert.ThrowsAny<ArgumentException>(() => new Author("Reader", email!));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    public void CommentRequiresContent(string? text) =>
        Assert.ThrowsAny<ArgumentException>(() => new Comment(new Author("Reader", "r@example.test"), text!, IPAddress.Loopback, ""));

    [Fact]
    public void CommentMaintainsBothSidesOfAuthorRelationship()
    {
        var author = new Author("Reader", "r@example.test");
        var comment = new Comment(author, "Привіт 😀", IPAddress.Loopback, "");
        Assert.Same(author, comment.Author);
        Assert.Equal(author.Id, comment.AuthorId);
        Assert.Same(comment, Assert.Single(author.Comments));
        Assert.NotEqual(Guid.Empty, comment.Id);
        Assert.Equal(TimeSpan.Zero, comment.CreatedAt.Offset);
    }

    [Fact]
    public void CallersCannotMutateAuthorComments()
    {
        var author = new Author("Reader", "r@example.test");
        var comment = new Comment(author, "Text", IPAddress.Loopback, "");
        var collection = Assert.IsAssignableFrom<ICollection<Comment>>(author.Comments);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => collection.Clear());
        Assert.Same(comment, Assert.Single(author.Comments));
    }

    [Fact]
    public void RequiredReferencesCannotBeNull()
    {
        var author = new Author("Reader", "r@example.test");
        Assert.Throws<ArgumentNullException>(() => new Comment(null!, "Text", IPAddress.Loopback, ""));
        Assert.Throws<ArgumentNullException>(() => new Comment(author, "Text", null!, ""));
        Assert.Throws<ArgumentNullException>(() => new Comment(author, "Text", IPAddress.Loopback, null!));
        Assert.Empty(author.Comments);
    }

    [Fact]
    public void StorageLimitsAreEnforcedBeforePersistence()
    {
        Assert.Throws<ArgumentException>(() => new Author(new string('A', 101), "r@example.test"));
        Assert.Throws<ArgumentException>(() => new Author("Reader", new string('a', 255)));
        Assert.Throws<ArgumentException>(() => new Author("Reader", "r@example.test", new string('a', 2049)));
        var author = new Author("Reader", "r@example.test");
        Assert.Throws<ArgumentException>(() => new Comment(author, new string('a', 65537), IPAddress.Loopback, ""));
        Assert.Throws<ArgumentException>(() => new Comment(author, "Text", IPAddress.Loopback, new string('a', 2049)));
        Assert.Empty(author.Comments);
    }
}
