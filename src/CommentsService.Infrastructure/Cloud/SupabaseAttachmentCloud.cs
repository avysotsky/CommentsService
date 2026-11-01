using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CommentsService.Application.Cloud;
using Microsoft.Extensions.Logging;
namespace CommentsService.Infrastructure.Cloud;

// Optional mirror: SQL is always read first; provider failures never change committed writes.
public sealed class SupabaseAttachmentCloud : IAttachmentCloud, IDisposable
{
    private readonly CloudSettings settings;
    private readonly HttpClient http;
    private readonly ILogger<SupabaseAttachmentCloud> logger;
    private readonly TimeProvider clock;
    private readonly SemaphoreSlim slots;
    private readonly object gate = new();
    private readonly Uri? endpoint;
    private readonly string? secret;
    private readonly string bucket;
    private readonly string partition;
    private long uploadBytes, downloadBytes;
    private int requests, failures;
    private bool stopped;
    private DateTimeOffset openUntil, privateUntil;
    public SupabaseAttachmentCloud(CloudSettings settings, HttpClient http, ILogger<SupabaseAttachmentCloud> logger,
        string database, string? url, string? secret, string? bucket, TimeProvider? clock = null)
    {
        this.settings = settings; this.http = http; this.logger = logger; this.clock = clock ?? TimeProvider.System;
        slots = new(settings.MaximumConcurrency); this.bucket = bucket ?? "";
        partition = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(database))).ToLowerInvariant()[..12];
        // Never accept arbitrary endpoints, credentials in URLs, redirects, or legacy JWT keys.
        if (url is not null && Regex.IsMatch(url, @"^https://[a-z0-9-]+\.supabase\.co/?$", RegexOptions.CultureInvariant) &&
            secret is not null && Regex.IsMatch(secret, @"^sb_secret_[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant) &&
            Regex.IsMatch(this.bucket, @"^[a-z0-9][a-z0-9-]{0,62}$", RegexOptions.CultureInvariant))
        { endpoint = new(url.TrimEnd('/') + "/storage/v1/"); this.secret = secret; }
    }
    public static HttpClient CreateClient() => new(new SocketsHttpHandler
    { AllowAutoRedirect = false, MaxConnectionsPerServer = 2, ConnectTimeout = TimeSpan.FromSeconds(2),
        PooledConnectionLifetime = TimeSpan.FromMinutes(5), AutomaticDecompression = DecompressionMethods.None })
        { Timeout = Timeout.InfiniteTimeSpan };
    public (long UploadBytes, long DownloadBytes, int Requests, bool Stopped) Statistics
    { get { lock (gate) return (uploadBytes, downloadBytes, requests, stopped); } }
    public string ObjectKey(CloudAttachment a) => $"v1/{partition}/{(a.Kind == AttachmentKind.Image ? "images" : "text")}/{a.Id:N}/{Convert.ToHexString(SHA256.HashData(a.Data)).ToLowerInvariant()}.{(a.Kind == AttachmentKind.Image ? "png" : "txt")}";
    private bool Eligible(CloudAttachment a) => settings.Enabled && endpoint is not null &&
        settings.AllowedCommentIds.Contains(a.CommentId) && a.Id != Guid.Empty &&
        a.Kind is AttachmentKind.Image or AttachmentKind.Text && a.Data.Length > 0 &&
        a.Data.Length <= (a.Kind == AttachmentKind.Image ? 524288 : 102400);
    private bool Available { get { lock (gate) return !stopped && clock.GetUtcNow() >= openUntil; } }
    private bool Reserve(int upload, int download)
    {
        lock (gate)
        {
            if (stopped || requests >= settings.MaximumRequests || uploadBytes + upload > settings.MaximumUploadBytes ||
                downloadBytes + download > settings.MaximumDownloadBytes) return false;
            requests++; uploadBytes += upload; downloadBytes += download; return true;
        }
    }
    private void Failed(HttpStatusCode? status = null)
    {
        lock (gate)
        {
            if (status is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.PaymentRequired)
                stopped = true;
            if (++failures >= settings.FailureThreshold || status == HttpStatusCode.TooManyRequests)
                openUntil = clock.GetUtcNow().AddSeconds(settings.BreakSeconds);
            privateUntil = default;
        }
        logger.LogWarning("Cloud attachment operation unavailable; SQL originals remain available");
    }
    private void Succeeded() { lock (gate) failures = 0; }
    private HttpRequestMessage Request(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, new Uri(endpoint!, path));
        request.Headers.Add("apikey", secret); return request;
    }
    private async Task<bool> EnsurePrivateAsync(CancellationToken token)
    {
        lock (gate) if (clock.GetUtcNow() < privateUntil) return true;
        if (!Reserve(0, 16385)) return false;
        using var request = Request(HttpMethod.Get, "bucket/" + bucket);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        if (!response.IsSuccessStatusCode) { Failed(response.StatusCode); return false; }
        var bytes = await BoundedAsync(response.Content, 16384, token);
        if (bytes is null) { Failed(); return false; }
        using var json = JsonDocument.Parse(bytes);
        var root = json.RootElement;
        if (!root.TryGetProperty("id", out var id) || id.GetString() != bucket ||
            !root.TryGetProperty("public", out var pub) || pub.ValueKind != JsonValueKind.False ||
            !root.TryGetProperty("allowed_mime_types", out var types) || types.ValueKind != JsonValueKind.Array ||
            !types.EnumerateArray().Select(x => x.GetString()).Order().SequenceEqual(new[] { "image/png", "text/plain" }) ||
            !root.TryGetProperty("file_size_limit", out var size) || !size.TryGetInt64(out var limit) || limit is < 524288 or > 5242880)
        { lock (gate) stopped = true; Failed(); return false; }
        lock (gate) privateUntil = clock.GetUtcNow().AddMinutes(5);
        return true;
    }
    private static async Task<byte[]?> BoundedAsync(HttpContent content, int maximum, CancellationToken token)
    {
        if (content.Headers.ContentLength > maximum) return null;
        await using var stream = await content.ReadAsStreamAsync(token);
        var buffer = new byte[maximum + 1]; var count = 0;
        while (count < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(count), token);
            if (read == 0) return buffer[..count];
            count += read;
        }
        return null;
    }
    public async Task<byte[]> ReadOrOriginalAsync(CloudAttachment a, CancellationToken token = default)
    {
        if (!Eligible(a) || !Available || !slots.Wait(0)) return a.Data;
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(settings.ReadTimeoutMilliseconds);
            if (!await EnsurePrivateAsync(timeout.Token) || !Reserve(0, a.Data.Length + 1)) return a.Data;
            using var request = Request(HttpMethod.Get, "object/authenticated/" + bucket + "/" + ObjectKey(a));
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode != HttpStatusCode.NotFound) Failed(response.StatusCode);
                return a.Data;
            }
            var bytes = await BoundedAsync(response.Content, a.Data.Length, timeout.Token);
            if (bytes is null || bytes.Length != a.Data.Length ||
                !CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), SHA256.HashData(a.Data)))
            { Failed(); return a.Data; }
            Succeeded(); logger.LogInformation("Cloud attachment read verified {AttachmentId}", a.Id);
            return bytes;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { Failed(); return a.Data; }
        catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or InvalidOperationException or ArgumentException)
        { Failed(); return a.Data; }
        finally { slots.Release(); }
    }
    public async Task<bool> CopyAsync(CloudAttachment a, CancellationToken token = default)
    {
        if (!Eligible(a)) return false;
        for (var attempt = 0; attempt < settings.MaximumAttempts && Available; attempt++)
        {
            var retry = false;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.WriteTimeoutSeconds));
            var entered = false;
            try
            {
                await slots.WaitAsync(timeout.Token); entered = true;
                if (!Available || !await EnsurePrivateAsync(timeout.Token) || !Reserve(a.Data.Length, 4096)) return false;
                using var request = Request(HttpMethod.Post, "object/" + bucket + "/" + ObjectKey(a));
                request.Headers.Add("x-upsert", "true");
                request.Content = new ByteArrayContent(a.Data);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(a.Kind == AttachmentKind.Image ? "image/png" : "text/plain");
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (response.IsSuccessStatusCode)
                {
                    if (await BoundedAsync(response.Content, 4096, timeout.Token) is null) { Failed(); return false; }
                    Succeeded(); logger.LogInformation("Cloud attachment copy completed {AttachmentId}", a.Id); return true;
                }
                Failed(response.StatusCode);
                retry = (int)response.StatusCode >= 500;
                if (response.StatusCode == HttpStatusCode.TooManyRequests && response.Headers.RetryAfter is { } after)
                {
                    var delay = after.Delta ?? (after.Date - clock.GetUtcNow());
                    lock (gate) openUntil = clock.GetUtcNow().AddSeconds(Math.Clamp(delay?.TotalSeconds ?? settings.BreakSeconds, 1, 3600));
                }
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) { Failed(); retry = true; }
            catch (Exception ex) when (ex is HttpRequestException or IOException or JsonException or InvalidOperationException or ArgumentException)
            { Failed(); retry = true; }
            finally { if (entered) slots.Release(); }
            if (!retry || !Available || attempt + 1 == settings.MaximumAttempts) break;
            await Task.Delay(settings.RetryDelayMilliseconds * (attempt + 1) + Random.Shared.Next(100), token);
        }
        return false;
    }
    public void Dispose() { http.Dispose(); slots.Dispose(); }
}
