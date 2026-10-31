using System.Net;
using CommentsService.Api.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CommentsService.Api.Tests;

public sealed class ProxyTests
{
    [Theory]
    [InlineData("192.0.2.10", "198.51.100.20", "https")]
    [InlineData("192.0.2.11", "192.0.2.11", "http")]
    public async Task HeadersAreAcceptedOnlyFromTheExplicitProxy(string peer, string expectedIp, string expectedScheme)
    {
        var settings = new ProxySettings { KnownProxies = ["192.0.2.10"] };
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(peer);
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.99, 198.51.100.20";
        context.Request.Headers["X-Forwarded-Proto"] = "http, https";
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance,
            Options.Create(settings.CreateOptions()));
        await middleware.Invoke(context);
        Assert.Equal(expectedIp, context.Connection.RemoteIpAddress!.ToString());
        Assert.Equal(expectedScheme, context.Request.Scheme);
    }

    [Theory]
    [InlineData("0.0.0.0")]
    [InlineData("::")]
    [InlineData("invalid")]
    public void InvalidTrustFailsValidation(string address) =>
        Assert.Throws<InvalidOperationException>(() => new ProxySettings { KnownProxies = [address] }.Validate());
}
