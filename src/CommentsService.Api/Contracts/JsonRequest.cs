using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;

namespace CommentsService.Api.Contracts;

internal static class JsonRequest
{
    private const int MaxBytes = 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        AllowDuplicateProperties = false,
        PropertyNameCaseInsensitive = false,
        MaxDepth = 16
    };

    public static async Task<T?> ReadAsync<T>(HttpContext context, CancellationToken token)
    {
        if (!context.Request.HasJsonContentType() || !MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var type) ||
            (type.CharSet is { Length: > 0 } charset && !charset.Trim('"').Equals("utf-8", StringComparison.OrdinalIgnoreCase)))
            throw new BadHttpRequestException("UTF-8 JSON is required.", StatusCodes.Status415UnsupportedMediaType);
        if (context.Request.ContentLength > MaxBytes) throw TooLarge();
        if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } size) size.MaxRequestBodySize = MaxBytes;
        // Also count actual bytes: this remains bounded without Content-Length or Kestrel features.
        using var body = new MemoryStream();
        var buffer = new byte[16384];
        int count;
        while ((count = await context.Request.Body.ReadAsync(buffer, token)) != 0)
        {
            if (body.Length + count > MaxBytes) throw TooLarge();
            await body.WriteAsync(buffer.AsMemory(0, count), token);
        }
        body.Position = 0;
        return await JsonSerializer.DeserializeAsync<T>(body, Options, token);
    }

    private static BadHttpRequestException TooLarge() => new("JSON request body exceeds 1 MiB.", StatusCodes.Status413PayloadTooLarge);
}
