using System.Net;
using CommentsService.Api.Contracts;
using CommentsService.Application.Comments.CreateComment;
using CommentsService.Application.Comments.GetComment;
using CommentsService.Application.Comments.GetLatestComments;

namespace CommentsService.Api.Endpoints;

public static class CommentsEndpoints
{
    public static IEndpointRouteBuilder MapCommentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/comments");
        group.MapPost("/preview", async (HttpContext context,
            CommentsService.Application.Comments.Markup.ICommentMarkup markup, CancellationToken token) =>
        {
            if (!context.Request.HasJsonContentType()) return Results.Problem(
                statusCode: StatusCodes.Status415UnsupportedMediaType, title: "Unsupported media type.");
            var body = await JsonRequest.ReadAsync<PreviewCommentBody>(context, token);
            if (body is null) return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid request body.");
            var result = markup.Validate(body.Text);
            return result.IsValid ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors.ToDictionary());
        });
        group.MapPost("", async (HttpContext context, ICreateComment create, CommentsService.Application.Common.ApplicationLimits limits, CancellationToken token) =>
        {
            if (!context.Request.HasJsonContentType() && !(context.Request.HasFormContentType &&
                context.Request.ContentType!.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase)))
                return Results.Problem(statusCode: 415, title: "Unsupported media type.");
            var input = await CreateCommentRequest.ReadAsync(context, token);
            if (input is null) return Results.Problem(statusCode: 400, title: "Invalid request body.");
            await using var imageStream = input.Image?.Content;
            await using var textStream = input.TextFile?.Content;
            var ip = context.Connection.RemoteIpAddress ?? IPAddress.Any;
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var client = new ClientMetadata(ip, userAgent[..Math.Min(userAgent.Length, limits.UserAgentMaxLength)]);
            var result = await create.ExecuteAsync(input, client, token);
            if (!result.IsValid) return Results.ValidationProblem(result.Errors.ToDictionary());
            return Results.Created($"/api/comments/{result.Value!.Id}", result.Value);
        });
        group.MapGet("/page", async (int? page, string? sort, string? direction,
            CommentsService.Application.Comments.GetRootPage.IGetRootPage get, CancellationToken token) =>
        {
            var result = await get.ExecuteAsync(page ?? 1, sort ?? "createdAt", direction ?? "desc", token);
            return result.IsValid ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors.ToDictionary());
        });
        group.MapGet("/{id:guid}/replies", async (Guid id, int? limit, Guid? after,
            CommentsService.Application.Comments.GetReplies.IGetReplies get, CommentsService.Application.Common.ApplicationLimits limits, CancellationToken token) =>
        {
            var result = await get.ExecuteAsync(id, limit ?? limits.DefaultListLimit, after, token);
            if (result.IsValid) return Results.Ok(result.Value);
            return result.Errors.ContainsKey("parentId") ? Results.Problem(statusCode: 404, title: "Comment was not found.") :
                Results.ValidationProblem(result.Errors.ToDictionary());
        });
        group.MapGet("/{id:guid}", async (Guid id, IGetComment get, CancellationToken token) =>
            await get.ExecuteAsync(id, token) is { } comment ? Results.Ok(comment) : Results.Problem(
                statusCode: StatusCodes.Status404NotFound, title: "Comment was not found."));
        group.MapGet("", async (int? limit, IGetLatestComments get, CommentsService.Application.Common.ApplicationLimits limits, CancellationToken token) =>
        {
            var result = await get.ExecuteAsync(limit ?? limits.DefaultListLimit, token);
            return result.IsValid ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors.ToDictionary());
        });
        return endpoints;
    }
}
