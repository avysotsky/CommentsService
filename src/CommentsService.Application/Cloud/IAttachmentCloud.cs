namespace CommentsService.Application.Cloud;

public interface IAttachmentCloud
{
    Task<byte[]> ReadOrOriginalAsync(CloudAttachment attachment, CancellationToken token = default);
    Task<bool> CopyAsync(CloudAttachment attachment, CancellationToken token = default);
}
public interface ICloudAttachmentSource
{
    Task<CloudAttachment?> FindByCommentAsync(Guid commentId, CancellationToken token = default);
}
public interface ICloudCopySignals
{
    IReadOnlyList<Guid> ReplayIds { get; }
    bool TryTake(out Guid commentId);
    void Complete(Guid commentId);
}
