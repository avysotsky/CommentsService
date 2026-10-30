namespace CommentsService.Api.Security;

public static class SecurityHeaders
{
    private const string ContentPolicy = "default-src 'none'; script-src 'self'; script-src-attr 'none'; style-src 'self'; img-src 'self' data: blob:; connect-src 'self'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            // TXT downloads intentionally retain their stricter sandbox policy.
            headers.TryAdd("Content-Security-Policy", ContentPolicy);
            if (context.Request.Path.StartsWithSegments("/api")) headers.CacheControl = "no-store";
            return Task.CompletedTask;
        });
        await next(context);
    });
}
