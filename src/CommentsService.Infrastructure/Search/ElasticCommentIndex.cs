using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CommentsService.Application.Search;
namespace CommentsService.Infrastructure.Search;

public sealed class ElasticCommentIndex(SearchSettings settings, HttpClient client, string? database = null) : ICommentSearchIndex, IDisposable
{
    private readonly string indexName = database is null ? settings.Index : SearchIndexName.ForDatabase(settings.Index, database);
    public void Dispose() => client.Dispose();
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static HttpClient CreateClient() => new(new SocketsHttpHandler
    { ConnectTimeout = TimeSpan.FromSeconds(1), MaxConnectionsPerServer = 4, PooledConnectionLifetime = TimeSpan.FromMinutes(2) });

    private async Task<JsonDocument> SendAsync(HttpMethod method, string path, byte[]? body, CancellationToken token, bool missingAllowed = false)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        try
        {
            var password = Environment.GetEnvironmentVariable("SEARCH_PASSWORD");
            if (string.IsNullOrEmpty(password)) throw new SearchUnavailableException();
            using var request = new HttpRequestMessage(method, settings.Endpoint.TrimEnd('/') + "/" + indexName + path);
            request.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(settings.Username + ":" + password)));
            if (body is not null) { request.Content = new ByteArrayContent(body); request.Content.Headers.ContentType = new(path.StartsWith("/_bulk", StringComparison.Ordinal) ? "application/x-ndjson" : "application/json"); }
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (missingAllowed && response.StatusCode == HttpStatusCode.NotFound) return JsonDocument.Parse("null");
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > 1048576) throw new SearchUnavailableException();
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var bytes = new MemoryStream(); var buffer = new byte[8192]; int count;
            while ((count = await stream.ReadAsync(buffer, deadline.Token)) != 0)
            {
                if (bytes.Length + count > 1048576) throw new SearchUnavailableException();
                bytes.Write(buffer, 0, count);
            }
            return JsonDocument.Parse(bytes.ToArray());
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException)
        { throw new SearchUnavailableException(); }
    }

    private async Task EnsureAsync(CancellationToken token)
    {
        using var map = await SendAsync(HttpMethod.Get, "/_mapping", null, token, true);
        if (map.RootElement.ValueKind != JsonValueKind.Null)
        {
            if (map.RootElement.ValueKind != JsonValueKind.Object || !map.RootElement.TryGetProperty(indexName, out var index) ||
                !Required(index,"mappings").TryGetProperty("_meta", out var meta) || !Required(meta,"commentSchema").TryGetInt32(out var version) || version != 1) throw new SearchUnavailableException();
            return;
        }
        var body = new { settings = new { number_of_shards = 1, number_of_replicas = 0, refresh_interval = "1s" }, mappings = new {
            dynamic = "strict", _source = new { enabled = false }, _meta = new { commentSchema = 1 }, properties = new {
                id = new { type = "keyword" }, userName = new { type = "text", analyzer = "standard" }, text = new { type = "text", analyzer = "standard" }, createdAt = new { type = "date" } } } };
        using var created = await SendAsync(HttpMethod.Put, "", JsonSerializer.SerializeToUtf8Bytes(body), token);
    }

    private static JsonElement Required(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(name,out var result)) throw new SearchUnavailableException();
        return result;
    }
    private static void RejectPartial(JsonElement value)
    {
        if (Required(value,"timed_out").ValueKind != JsonValueKind.False) throw new SearchUnavailableException();
    }
    private static Guid[] ReadIds(JsonElement value, int limit)
    {
        var hits=Required(Required(value,"hits"),"hits");
        if (hits.ValueKind != JsonValueKind.Array) throw new SearchUnavailableException();
        var ids=new List<Guid>();
        foreach(var hit in hits.EnumerateArray().Take(limit))
        {
            var item=Required(hit,"_id");
            if(item.ValueKind!=JsonValueKind.String || !Guid.TryParse(item.GetString(),out var id)) throw new SearchUnavailableException();
            ids.Add(id);
        }
        return ids.Distinct().ToArray();
    }

    public async Task<SearchIds> SearchAsync(string query, int page, int pageSize, CancellationToken token)
    {
        await EnsureAsync(token);
        var body = new Dictionary<string,object?> { ["from"] = (page-1)*pageSize, ["size"] = pageSize+1, ["_source"] = false,
            ["track_total_hits"] = false, ["timeout"] = settings.TimeoutSeconds*1000+"ms",
            ["query"] = new { multi_match = new { query, fields = new[] { "text", "userName^2" }, type = "best_fields", @operator = "and" } },
            ["sort"] = new object[] { new { _score = "desc" }, new { createdAt = "desc" }, new { id = "asc" } } };
        using var response = await SendAsync(HttpMethod.Post, "/_search?allow_partial_search_results=false", JsonSerializer.SerializeToUtf8Bytes(body), token);
        RejectPartial(response.RootElement);
        var ids = ReadIds(response.RootElement, pageSize+1);
        return new(ids.Take(pageSize).ToArray(), ids.Length > pageSize);
    }

    public async Task IndexAsync(IReadOnlyList<SearchDocument> documents, CancellationToken token)
    {
        await EnsureAsync(token);
        using var bytes = new MemoryStream();
        foreach (var document in documents)
        {
            var header = JsonSerializer.SerializeToUtf8Bytes(new { index = new { _id = document.Id.ToString("D") } });
            var value = JsonSerializer.SerializeToUtf8Bytes(new { id = document.Id, userName = document.UserName, text = SearchDocumentText.Extract(document.Text), createdAt = document.CreatedAt }, Json);
            if (bytes.Length + header.Length + value.Length + 2 > 1048576) { await BulkAsync(bytes.ToArray(), token, true); bytes.SetLength(0); bytes.Position=0; }
            bytes.Write(header); bytes.WriteByte(10); bytes.Write(value); bytes.WriteByte(10);
        }
        if (bytes.Length != 0) await BulkAsync(bytes.ToArray(), token, true);
    }
    private async Task BulkAsync(byte[] bytes, CancellationToken token, bool checkSize)
    {
        if (checkSize)
        {
            using var size = await SendAsync(HttpMethod.Get, "/_stats/store?filter_path=_all.primaries.store.size_in_bytes", null, token);
            if (size.RootElement.GetProperty("_all").GetProperty("primaries").GetProperty("store").GetProperty("size_in_bytes").GetInt64() >= settings.MaximumIndexBytes) throw new SearchUnavailableException();
        }
        using var result = await SendAsync(HttpMethod.Post, "/_bulk?filter_path=errors&refresh=wait_for", bytes, token);
        if (result.RootElement.GetProperty("errors").GetBoolean()) throw new SearchUnavailableException();
    }
    public async Task<IReadOnlyList<Guid>> ScanIdsAsync(Guid? after, int limit, CancellationToken token)
    {
        await EnsureAsync(token);
        var body = new Dictionary<string,object?> { ["size"] = limit, ["_source"] = false, ["track_total_hits"] = false, ["sort"] = new[] { new { id = "asc" } } };
        if (after is not null) body["search_after"] = new[] { after.Value.ToString("D") };
        using var result = await SendAsync(HttpMethod.Post, "/_search?allow_partial_search_results=false", JsonSerializer.SerializeToUtf8Bytes(body), token);
        RejectPartial(result.RootElement);
        return ReadIds(result.RootElement, limit);
    }
    public async Task DeleteAsync(IReadOnlyList<Guid> ids, CancellationToken token)
    {
        if (ids.Count == 0) return;
        var text = string.Join("\n", ids.Select(id => JsonSerializer.Serialize(new { delete = new { _id = id.ToString("D") } }))) + "\n";
        await BulkAsync(Encoding.UTF8.GetBytes(text), token, false);
    }
}
