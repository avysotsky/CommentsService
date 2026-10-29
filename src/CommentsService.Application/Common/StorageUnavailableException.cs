namespace CommentsService.Application.Common;

public sealed class StorageUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
