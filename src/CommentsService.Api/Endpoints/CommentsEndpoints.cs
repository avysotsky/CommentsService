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
            var body = await context.Request.ReadFromJsonAsync<PreviewCommentBody>(token);
            if (body is null) return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid request body.");
            var result = markup.Validate(body.Text);
            return result.IsValid ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors.ToDictionary());
        });
        group.MapPost("", async (HttpContext context, ICreateComment create, CancellationToken token) =>
        {
            if (!context.Request.HasJsonContentType()) return Results.Problem(
                statusCode: StatusCodes.Status415UnsupportedMediaType, title: "Unsupported media type.");
            var body = await context.Request.ReadFromJsonAsync<CreateCommentBody>(token);
            if (body is null) return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid request body.");
            var ip = context.Connection.RemoteIpAddress ?? IPAddress.Any;
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var client = new ClientMetadata(ip, userAgent[..Math.Min(userAgent.Length, ClientMetadata.UserAgentMaxLength)]);
            var result = await create.ExecuteAsync(new(body.UserName, body.Email, body.HomePage, body.Text, body.ParentId), client, token);
            if (!result.IsValid) return Results.ValidationProblem(result.Errors.ToDictionary());
            return Results.Created($"/api/comments/{result.Value!.Id}", result.Value);
        });
        group.MapGet("/{id:guid}/replies", async (Guid id, int? limit, Guid? after,
            CommentsService.Application.Comments.GetReplies.IGetReplies get, CancellationToken token) =>
        {
            var result = await get.ExecuteAsync(id, limit ?? 25, after, token);
            if (result.IsValid) return Results.Ok(result.Value);
            return result.Errors.ContainsKey("parentId") ? Results.Problem(statusCode: 404, title: "Comment was not found.") :
                Results.ValidationProblem(result.Errors.ToDictionary());
        });
        group.MapGet("/{id:guid}", async (Guid id, IGetComment get, CancellationToken token) =>
            await get.ExecuteAsync(id, token) is { } comment ? Results.Ok(comment) : Results.Problem(
                statusCode: StatusCodes.Status404NotFound, title: "Comment was not found."));
        group.MapGet("", async (int? limit, IGetLatestComments get, CancellationToken token) =>
        {
            var result = await get.ExecuteAsync(limit ?? GetLatestComments.DefaultLimit, token);
            return result.IsValid ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors.ToDictionary());
        });
        return endpoints;
    }
}
