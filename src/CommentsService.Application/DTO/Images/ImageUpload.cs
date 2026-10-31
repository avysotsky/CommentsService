namespace CommentsService.Application.Images;

public sealed record ImageUpload(string FileName, long Length, Stream Content);
