using System.Net;
using System.Net.Http.Json;
using CommentsService.Application.Comments.GetRootPage;
using CommentsService.Infrastructure.Caching;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed partial class CommentsHttpTests
{
    [Fact]
    public async Task PageHitIsInvalidatedByRealCommittedPost()
    {
        var cache = fixture.Factory.Services.GetRequiredService<CommentReadCache>();
        var first = await fixture.Client.GetFromJsonAsync<RootCommentsPage>("/api/comments/page");
        var hits = cache.Statistics.Hits;
        await fixture.Client.GetAsync("/api/comments/page");
        Assert.True(cache.Statistics.Hits > hits);
        using var response = await fixture.PostAsync(ApiFixture.Payload("Cache event integration"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var second = await fixture.Client.GetFromJsonAsync<RootCommentsPage>("/api/comments/page");
        Assert.True(second!.TotalCount > first!.TotalCount);
        Assert.Contains(second.Items, item => item.Text == "Cache event integration");
    }
}
