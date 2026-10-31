using CommentsService.Application.Search;
using CommentsService.Api.Configuration;
using CommentsService.Api.Search;
namespace CommentsService.Api.Endpoints;
public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/comments/search", async (string? query, int? page, ISearchComments search, RuntimeSettings runtime, SearchCapacity capacity, HttpContext context, CancellationToken token) => {
            if (!runtime.Search.Enabled) return Results.NotFound();
            if (!await capacity.Slots.WaitAsync(0,token)) return Results.StatusCode(429);
            try
            {
                var result=await search.ExecuteAsync(query,page??1,token);
                return result.IsValid ? Results.Ok(result.Value) : Results.ValidationProblem(result.Errors.ToDictionary());
            }
            finally { capacity.Slots.Release(); }
        });
    }
}
