using CommentsService.Application.TextFiles;
namespace CommentsService.Api.Endpoints;
public static class TextFileEndpoints
{
    public static IEndpointRouteBuilder MapTextFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/text-files/{id:guid}", async (Guid id, IGetTextFile get, HttpContext context, CancellationToken token) =>
        {
            var file = await get.ExecuteAsync(id, token);
            if (file is null) return Results.NotFound();
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            context.Response.Headers.CacheControl = "no-store";
            return Results.File(file.Data, "text/plain; charset=utf-8", fileDownloadName: $"{id:N}.txt");
        });
        return endpoints;
    }
}
