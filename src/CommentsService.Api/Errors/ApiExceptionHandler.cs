using CommentsService.Application.Common;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace CommentsService.Api.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            StorageUnavailableException => StatusCodes.Status503ServiceUnavailable,
            BadHttpRequestException request => request.StatusCode,
            JsonException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
        if (status >= 500) logger.LogError("Request failed with status {Status}; trace {TraceId}; type {ErrorType}.", status, context.TraceIdentifier, exception.GetType().Name);
        else logger.LogInformation("Request rejected with status {Status}; trace {TraceId}.", status, context.TraceIdentifier);
        await Results.Problem(statusCode: status, title: status switch
        {
            503 => "Comments storage is unavailable.",
            400 => "Invalid request body.",
            413 => "Request body is too large.",
            415 => "Unsupported media type.",
            _ => "An unexpected error occurred."
        }).ExecuteAsync(context);
        return true;
    }
}
