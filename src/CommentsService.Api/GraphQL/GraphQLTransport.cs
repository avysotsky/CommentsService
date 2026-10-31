using System.Text.Json;
using CommentsService.Api.Configuration;
using CommentsService.Api.Contracts;
using CommentsService.Api.DTO.GraphQL;

namespace CommentsService.Api.GraphQL;

public sealed class GraphQLCapacity(GraphQLSettings settings) : IDisposable
{
    public SemaphoreSlim Slots { get; } = new(settings.MaximumConcurrentRequests);
    public void Dispose() => Slots.Dispose();
}

public static class GraphQLTransport
{
    public static IApplicationBuilder UseCommentGraphQLTransport(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        if (!context.Request.Path.StartsWithSegments("/api/graphql", StringComparison.OrdinalIgnoreCase)) { await next(context); return; }
        var settings = context.RequestServices.GetRequiredService<RuntimeSettings>().GraphQL;
        if (!settings.Enabled) { context.Response.StatusCode = 404; return; }
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            context.Response.Headers.Allow = "POST"; context.Response.StatusCode = 405; return;
        }
        // Strict single JSON envelope; no GET, multipart, extensions/APQ, request/variable batching or socket transport.
        var request = await JsonRequest.ReadAsync<GraphQLRequest>(context, context.RequestAborted, settings.MaximumRequestBytes);
        if (request is null || string.IsNullOrWhiteSpace(request.Query) ||
            request.Variables is { ValueKind: not (JsonValueKind.Object or JsonValueKind.Null) })
        { context.Response.StatusCode = 400; return; }
        var capacity = context.RequestServices.GetRequiredService<GraphQLCapacity>();
        if (!await capacity.Slots.WaitAsync(0, context.RequestAborted)) { context.Response.StatusCode = 429; return; }
        var original = context.Request.Body;
        await using var body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        try
        {
            context.Request.Body = body;
            context.Request.ContentLength = body.Length;
            await next(context);
        }
        finally { context.Request.Body = original; capacity.Slots.Release(); }
    });
}
