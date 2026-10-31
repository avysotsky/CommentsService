using CommentsService.Api.Realtime;

namespace CommentsService.Api.Endpoints;

public static class RealtimeEndpoints
{
    public static IEndpointRouteBuilder MapRealtimeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/comments/live", LiveCommentConnection.AcceptAsync);
        return endpoints;
    }
}
