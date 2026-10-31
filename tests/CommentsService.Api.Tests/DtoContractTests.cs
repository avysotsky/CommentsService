using System.Text.Json;
using CommentsService.Api.DTO.Responses;
using CommentsService.Application.Comments;
using CommentsService.Application.Comments.GetRootPage;
using CommentsService.Application.Comments.GetReplies;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed class DtoContractTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void CommentWireShapeExposesOnlyPublicMetadata()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var comment = new CommentDto(id, "Reader", "reader@example.test", null, "hello",
            DateTimeOffset.UnixEpoch, null, id, new(id, "image.png", 320, 240), new(id, "file.txt", 10));
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(comment, Options));
        Assert.Equal(new[] { "id", "userName", "email", "homePage", "text", "createdAt", "parentId", "rootId", "image", "textFile" },
            json.RootElement.EnumerateObject().Select(property => property.Name));
        Assert.Equal(new[] { "id", "fileName", "width", "height" },
            json.RootElement.GetProperty("image").EnumerateObject().Select(property => property.Name));
        Assert.Equal(new[] { "id", "fileName", "sizeBytes" },
            json.RootElement.GetProperty("textFile").EnumerateObject().Select(property => property.Name));
        Assert.Equal(comment, JsonSerializer.Deserialize<CommentDto>(json.RootElement.GetRawText(), Options));
    }

    [Fact]
    public void PagesAndSystemResponsesRetainExactJson()
    {
        Assert.Equal("{\"items\":[],\"page\":1,\"pageSize\":25,\"totalCount\":0,\"totalPages\":0}",
            JsonSerializer.Serialize(new RootCommentsPage([], 1, 25, 0, 0), Options));
        Assert.Equal("{\"items\":[],\"nextAfter\":null}", JsonSerializer.Serialize(new RepliesPage([], null), Options));
        Assert.Equal("{\"status\":\"ok\"}", JsonSerializer.Serialize(new HealthResponse("ok"), Options));
        Assert.Equal("{\"application\":\"CommentsService\",\"version\":\"1.1.0.15\",\"stage\":\"DtoContracts\"}",
            JsonSerializer.Serialize(new ApplicationStatus("CommentsService", "1.1.0.15", "DtoContracts"), Options));
        Assert.Equal("{\"id\":\"00000000-0000-0000-0000-000000000000\",\"imageDataUrl\":\"data:image/png;base64,AA==\",\"expiresAt\":\"1970-01-01T00:00:00+00:00\"}",
            JsonSerializer.Serialize(new CaptchaResponse(Guid.Empty, "data:image/png;base64,AA==", DateTimeOffset.UnixEpoch), Options));
    }
}
