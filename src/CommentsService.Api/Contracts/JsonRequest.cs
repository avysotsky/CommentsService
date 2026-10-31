using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;

namespace CommentsService.Api.Contracts;

internal static class JsonRequest
{
    public static async Task<T?> ReadAsync<T>(HttpContext context, CancellationToken token)
    {
        var settings = context.RequestServices.GetRequiredService<CommentsService.Api.Configuration.RuntimeSettings>().Requests;
        var maxBytes = settings.JsonMaxBytes;
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            AllowDuplicateProperties = false, PropertyNameCaseInsensitive = false, MaxDepth = settings.JsonMaxDepth
        };
        if (!context.Request.HasJsonContentType() || !MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var type) ||
            (type.CharSet is { Length: > 0 } charset && !charset.Trim('"').Equals("utf-8", StringComparison.OrdinalIgnoreCase)))
            throw new BadHttpRequestException("UTF-8 JSON is required.", StatusCodes.Status415UnsupportedMediaType);
        if (context.Request.ContentLength > maxBytes) throw TooLarge();
        if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } size) size.MaxRequestBodySize = maxBytes;
        // Also count actual bytes: this remains bounded without Content-Length or Kestrel features.
        using var body = new MemoryStream();
        var buffer = new byte[16384];
        int count;
        while ((count = await context.Request.Body.ReadAsync(buffer, token)) != 0)
        {
            if (body.Length + count > maxBytes) throw TooLarge();
            await body.WriteAsync(buffer.AsMemory(0, count), token);
        }
        body.Position = 0;
        return await JsonSerializer.DeserializeAsync<T>(body, options, token);
    }

    private static BadHttpRequestException TooLarge() => new("JSON request body exceeds the configured limit.", StatusCodes.Status413PayloadTooLarge);
}
