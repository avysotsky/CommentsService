namespace CommentsService.Application.Events;

public interface ICommentEvents
{
    // Implementations isolate subscriber failures. Called only after persistence succeeds.
    void Publish(CommentCreated notification);
}

public interface ICommentCreatedHandler
{
    void Handle(CommentCreated notification);
}
