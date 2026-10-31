using CommentsService.Application.Events;
using CommentsService.Infrastructure.Events;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CommentsService.Data.Tests;

public sealed class CommentEventsTests
{
    [Fact]
    public void FailedSubscriberDoesNotPreventOtherSubscribersOrEscapeAfterCommit()
    {
        var successful = new Capture();
        var dispatcher = new CommentEvents([new Failing(), successful], NullLogger<CommentEvents>.Instance);
        var notification = new CommentCreated(Guid.NewGuid(), Guid.NewGuid(), null, Guid.NewGuid(), DateTimeOffset.UtcNow);
        dispatcher.Publish(notification);
        Assert.Same(notification, successful.Value);
    }

    private sealed class Failing : ICommentCreatedHandler
    { public void Handle(CommentCreated notification) => throw new InvalidOperationException("private content"); }
    private sealed class Capture : ICommentCreatedHandler
    {
        public CommentCreated? Value { get; private set; }
        public void Handle(CommentCreated notification) => Value = notification;
    }
}
