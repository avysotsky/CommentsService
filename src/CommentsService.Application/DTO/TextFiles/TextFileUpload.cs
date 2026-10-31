namespace CommentsService.Application.TextFiles;

public sealed record TextFileUpload(string FileName, long Length, Stream Content);
