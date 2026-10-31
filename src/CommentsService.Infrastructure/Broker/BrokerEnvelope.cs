using System.Text.Json;
using System.Text.Json.Serialization;
using CommentsService.Application.Events;

namespace CommentsService.Infrastructure.Broker;

public sealed record BrokerEnvelope(int Version, Guid OriginId, CommentCreated Event);

public static class BrokerEnvelopeCodec
{
    public const string MessageType = "comments.created.v1";
    public const int MaximumBytes = 1024;
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false, AllowDuplicateProperties = false, MaxDepth = 4,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    public static byte[] Encode(BrokerEnvelope value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);
    public static BrokerEnvelope? Decode(ReadOnlyMemory<byte> bytes)
    {
        if (bytes.Length is < 1 or > MaximumBytes) return null;
        try
        {
            var value = JsonSerializer.Deserialize<BrokerEnvelope>(bytes.Span, Options);
            if (value is null || value.Version != 1 || value.OriginId == Guid.Empty || value.Event is not { } e ||
                e.EventId == Guid.Empty || e.CommentId == Guid.Empty || e.RootId == Guid.Empty ||
                e.CreatedAt == default || e.ParentId == Guid.Empty || (e.ParentId is null && e.RootId != e.CommentId)) return null;
            return value;
        }
        catch (JsonException) { return null; }
    }
}
