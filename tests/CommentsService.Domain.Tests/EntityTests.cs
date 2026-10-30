using System.Net;
using CommentsService.Domain.Entities;
using Xunit;

namespace CommentsService.Domain.Tests;

public sealed class EntityTests
{
    [Fact]
    public void RepliesKeepImmutableParentAndRootWithoutDepthLimit()
    {
        var root = new Comment(new Author("Root", "r@example.test"), "root", IPAddress.Loopback, "");
        Assert.Null(root.ParentId); Assert.Equal(root.Id, root.RootId);
        var parent = root;
        for (var depth = 0; depth < 1000; depth++)
        {
            var child = new Comment(new Author("Reply", "r@example.test"), "reply", IPAddress.Loopback, "", parent);
            Assert.Equal(parent.Id, child.ParentId); Assert.Equal(root.Id, child.RootId);
            parent = child;
        }
    }

    [Fact]
    public void ImageOwnsItsBytesAndOnlyOneCanAttachToAComment()
    {
        var comment=new Comment(new Author("Reader","r@example.test"),"text",IPAddress.Loopback,"");
        byte[] data=[1,2,3]; var image=new ImageAttachment(comment,"picture.png",data,320,240);
        data[0]=9; var exposed=image.Data; exposed[0]=8;
        Assert.Equal(1,image.Data[0]); Assert.Same(image,comment.Image); Assert.Equal(comment.Id,image.CommentId);
        Assert.Throws<InvalidOperationException>(()=>new ImageAttachment(comment,"again.png",[1],1,1));
    }
    [Theory]
    [InlineData(0,1)] [InlineData(321,1)] [InlineData(1,241)]
    public void ImageBoundsAreDomainInvariants(int width,int height)
    {
        var comment=new Comment(new Author("Reader","r@example.test"),"text",IPAddress.Loopback,"");
        Assert.Throws<ArgumentException>(()=>new ImageAttachment(comment,"picture.png",[1],width,height)); Assert.Null(comment.Image);
    }

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
