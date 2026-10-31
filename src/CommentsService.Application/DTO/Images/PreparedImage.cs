namespace CommentsService.Application.Images;

public sealed record PreparedImage(string FileName, byte[] Data, int Width, int Height);
