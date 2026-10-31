using CommentsService.Application.Captcha;
using CommentsService.Api.DTO.Responses;
namespace CommentsService.Api.Endpoints;

public static class CaptchaEndpoints
{
    public static IEndpointRouteBuilder MapCaptchaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/captcha", (HttpContext context, ICaptchaIssuer issuer, Guid? replace) =>
        {
            context.Response.Headers.CacheControl = "no-store, no-cache";
            context.Response.Headers.Pragma = "no-cache";
            try
            {
                var challenge = issuer.Issue(replace);
                return Results.Ok(new CaptchaResponse(challenge.Id, "data:image/png;base64," + Convert.ToBase64String(challenge.Image), challenge.ExpiresAt));
            }
            catch (CaptchaCapacityException) { return Results.Problem(statusCode: 429, title: "CAPTCHA capacity reached. Try again later."); }
        });
        return endpoints;
    }
}
