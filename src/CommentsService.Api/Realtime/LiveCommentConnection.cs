using System.Net.WebSockets;
using System.Text;

namespace CommentsService.Api.Realtime;

public static class LiveCommentConnection
{
    private static readonly byte[] Connected = Encoding.UTF8.GetBytes("{\"type\":\"connected\"}");
    private static readonly byte[] Changed = Encoding.UTF8.GetBytes("{\"type\":\"commentsChanged\"}");

    public static async Task AcceptAsync(HttpContext context, LiveCommentBroadcaster broadcaster,
        RealtimeSettings settings, IHostApplicationLifetime lifetime)
    {
        if (!settings.Enabled) { context.Response.StatusCode = 404; return; }
        if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
        // Browsers must originate from this exact site; no cookies or client payloads are used.
        var expected = new Uri($"{context.Request.Scheme}://{context.Request.Host}");
        if (!Uri.TryCreate(context.Request.Headers.Origin.ToString(), UriKind.Absolute, out var origin) ||
            origin.AbsolutePath != "/" || origin.Query.Length != 0 || origin.Fragment.Length != 0 ||
            origin.UserInfo.Length != 0 || !string.Equals(origin.GetLeftPart(UriPartial.Authority), expected.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
        { context.Response.StatusCode = 403; return; }
        using var subscription = broadcaster.TrySubscribe();
        if (subscription is null) { context.Response.StatusCode = 503; return; }
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted, lifetime.ApplicationStopping);
        try
        {
            await SendAsync(socket, Connected, settings, stop.Token);
            var receive = ReceiveAsync(socket, stop.Token);
            var send = SendChangesAsync(socket, subscription, settings, stop.Token);
            await Task.WhenAny(receive, send);
            await stop.CancelAsync();
            try { await Task.WhenAll(receive, send); }
            catch (Exception error) when (error is WebSocketException or OperationCanceledException or IOException) { }
            if (socket.State == WebSocketState.CloseReceived)
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(settings.SendTimeoutSeconds));
                await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", timeout.Token);
            }
        }
        catch (Exception error) when (error is WebSocketException or OperationCanceledException or IOException) { }
        finally { socket.Abort(); }
    }

    private static async Task ReceiveAsync(WebSocket socket, CancellationToken token)
    {
        // This is a server-to-client invalidation protocol. Application messages are not accepted.
        var buffer = new byte[1];
        await socket.ReceiveAsync(buffer.AsMemory(), token);
    }

    private static async Task SendChangesAsync(WebSocket socket, LiveCommentBroadcaster.Subscription subscription,
        RealtimeSettings settings, CancellationToken token)
    {
        await foreach (var hint in subscription.Reader.ReadAllAsync(token))
            await SendAsync(socket, Changed, settings, token);
    }

    private static async Task SendAsync(WebSocket socket, byte[] data, RealtimeSettings settings, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.SendTimeoutSeconds));
        await socket.SendAsync(data.AsMemory(), WebSocketMessageType.Text, true, timeout.Token);
    }
}
