using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace CommentsService.Api.Configuration;

public sealed class ProxySettings
{
    public string[] KnownProxies { get; set; } = [];

    public void Validate()
    {
        if (KnownProxies is null || KnownProxies.Length > 16 ||
            KnownProxies.Any(address => !IPAddress.TryParse(address, out var parsed) ||
                parsed.Equals(IPAddress.Any) || parsed.Equals(IPAddress.IPv6Any)))
            throw new InvalidOperationException("Invalid trusted proxy addresses.");
    }

    public ForwardedHeadersOptions CreateOptions()
    {
        Validate();
        if (KnownProxies.Length == 0) throw new InvalidOperationException("At least one trusted proxy is required.");
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1,
            RequireHeaderSymmetry = true
        };
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        foreach (var address in KnownProxies) options.KnownProxies.Add(IPAddress.Parse(address));
        return options;
    }
}
