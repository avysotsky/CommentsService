using System.Net.Http.Json;
using System.Text.Json;
using CommentsService.Application.Comments;
using CommentsService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed class ApiFixture : IAsyncLifetime
{
    private readonly HashSet<Guid> owned = [];
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("PGDATABASE") != "comments_test")
            throw new InvalidOperationException("HTTP integration tests require PGDATABASE=comments_test.");
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Testing"));
        Client = Factory.CreateClient();
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("CommentsService.Api.Tests/1.0");
        Client.DefaultRequestHeaders.Add("X-Forwarded-For", "203.0.113.79");
        return Task.CompletedTask;
    }

    public static Dictionary<string, object?> Payload(string? text = "Hello") => new()
    {
        ["userName"] = "Api" + Guid.NewGuid().ToString("N"),
        ["email"] = Guid.NewGuid().ToString("N") + "@example.test",
        ["homePage"] = null,
        ["text"] = text
    };

    public async Task<HttpResponseMessage> PostAsync(Dictionary<string, object?> payload) =>
        await SendAsync(JsonContent.Create(payload));

    public async Task<HttpResponseMessage> SendAsync(HttpContent content, string? userAgent = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/comments") { Content = content };
        if (userAgent is not null) request.Headers.UserAgent.ParseAdd(userAgent);
        var response = await Client.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.Created)
        {
            var value = JsonSerializer.Deserialize<CommentDto>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (value is not null) owned.Add(value.Id);
        }
        return response;
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        if (owned.Count == 0) return;
        await using var db = new CommentsDbContextFactory().CreateDbContext([]);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var authorIds = await db.Comments.Where(comment => owned.Contains(comment.Id)).Select(comment => comment.AuthorId).ToArrayAsync();
        await db.Comments.Where(comment => owned.Contains(comment.Id)).ExecuteDeleteAsync();
        await db.Authors.Where(author => authorIds.Contains(author.Id) && !db.Comments.Any(comment => comment.AuthorId == author.Id)).ExecuteDeleteAsync();
        await transaction.CommitAsync();
    }
}
