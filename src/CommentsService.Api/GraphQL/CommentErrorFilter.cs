using CommentsService.Application.Common;
using HotChocolate;
using HotChocolate.Execution;

namespace CommentsService.Api.GraphQL;

public sealed class CommentErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        if (error.Exception is null) return error;
        return ErrorBuilder.New()
            .SetMessage(error.Exception is StorageUnavailableException ? "Comments storage is unavailable." : "Comment query failed.")
            .SetCode(error.Exception is StorageUnavailableException ? "STORAGE_UNAVAILABLE" : "QUERY_FAILED")
            .SetPath(error.Path).Build();
    }
}
