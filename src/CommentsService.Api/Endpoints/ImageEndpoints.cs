using CommentsService.Application.Images;
namespace CommentsService.Api.Endpoints;

public static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/images/{id:guid}", async (Guid id, bool? download, IGetImage get, HttpContext context, CancellationToken token) =>
        {
            var image = await get.ExecuteAsync(id, token);
            if (image is null) return Results.NotFound();
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers.ContentDisposition = $"{(download == true ? "attachment" : "inline")}; filename=\"{id:N}.png\"";
            return Results.File(image.Data, "image/png");
        });
        return endpoints;
    }
}
