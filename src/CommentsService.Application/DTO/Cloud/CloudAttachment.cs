namespace CommentsService.Application.Cloud;

public enum AttachmentKind { Image, Text }
public sealed record CloudAttachment(Guid Id, Guid CommentId, AttachmentKind Kind, byte[] Data);
