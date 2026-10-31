using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommentsService.Infrastructure.Broker;

public sealed class RabbitCommentBroker(BrokerSettings settings, BrokerNotificationRouter router, ILogger<RabbitCommentBroker> logger)
{
    public async Task RunSessionAsync(CancellationToken token)
    {
        var password = Environment.GetEnvironmentVariable("BROKER_PASSWORD");
        if (string.IsNullOrEmpty(password)) throw new InvalidOperationException("Broker secret is unavailable.");
        var factory = new ConnectionFactory
        {
            HostName = settings.Host, Port = settings.Port, UserName = settings.Username, Password = password,
            AutomaticRecoveryEnabled = false, TopologyRecoveryEnabled = false,
            MaxInboundMessageBodySize = BrokerEnvelopeCodec.MaximumBytes,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(settings.OperationTimeoutSeconds),
            ContinuationTimeout = TimeSpan.FromSeconds(settings.OperationTimeoutSeconds),
            HandshakeContinuationTimeout = TimeSpan.FromSeconds(settings.OperationTimeoutSeconds),
            RequestedHeartbeat = TimeSpan.FromSeconds(5), ConsumerDispatchConcurrency = 1
        };
        await using var connection = await factory.CreateConnectionAsync(token);
        await using var publisher = await connection.CreateChannelAsync(new CreateChannelOptions(true, true), token);
        await using var consumerChannel = await connection.CreateChannelAsync(cancellationToken: token);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionShutdownAsync += (_, _) => { closed.TrySetResult(); return Task.CompletedTask; };
        publisher.ChannelShutdownAsync += (_, _) => { closed.TrySetResult(); return Task.CompletedTask; };
        consumerChannel.ChannelShutdownAsync += (_, _) => { closed.TrySetResult(); return Task.CompletedTask; };
        await publisher.ExchangeDeclareAsync(settings.Exchange, ExchangeType.Topic, durable: false, autoDelete: false, cancellationToken: token);
        var queue = await consumerChannel.QueueDeclareAsync("", durable: false, exclusive: true, autoDelete: true,
            arguments: new Dictionary<string, object?> { ["x-max-length"] = settings.Capacity, ["x-max-length-bytes"] = settings.Capacity * BrokerEnvelopeCodec.MaximumBytes,
                ["x-message-ttl"] = 15000, ["x-overflow"] = "reject-publish" }, cancellationToken: token);
        await consumerChannel.QueueBindAsync(queue.QueueName, settings.Exchange, BrokerEnvelopeCodec.MessageType, cancellationToken: token);
        await consumerChannel.BasicQosAsync(0, 16, false, token);
        var consumer = new AsyncEventingBasicConsumer(consumerChannel);
        consumer.UnregisteredAsync += (_, _) => { closed.TrySetResult(); return Task.CompletedTask; };
        consumer.ReceivedAsync += async (_, delivery) =>
        {
            var value = delivery.BasicProperties.Type == BrokerEnvelopeCodec.MessageType && delivery.BasicProperties.ContentType == "application/json"
                ? BrokerEnvelopeCodec.Decode(delivery.Body) : null;
            if (value is null)
            {
                logger.LogWarning("Broker rejected an unsupported notification envelope");
                await consumerChannel.BasicNackAsync(delivery.DeliveryTag, false, false, lifetime.Token);
                return;
            }
            router.Receive(value);
            await consumerChannel.BasicAckAsync(delivery.DeliveryTag, false, lifetime.Token);
            logger.LogInformation("Broker notification received {EventId}", value.Event.EventId);
        };
        await consumerChannel.BasicConsumeAsync(queue.QueueName, false, consumer, token);
        router.SetConnected(true);
        logger.LogInformation("Broker connected; disposable notification subscription ready");
        var publish = PublishAsync(publisher, lifetime.Token);
        try
        {
            await Task.WhenAny(publish, closed.Task);
            if (publish.IsCompleted) await publish;
        }
        finally
        {
            router.SetConnected(false);
            await lifetime.CancelAsync();
            try { await publish; } catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        }
    }

    private async Task PublishAsync(IChannel channel, CancellationToken token)
    {
        await foreach (var notification in router.Reader.ReadAllAsync(token))
        {
            if (!router.ShouldPublish(notification.EventId)) continue;
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(settings.OperationTimeoutSeconds));
            var bytes = BrokerEnvelopeCodec.Encode(new(1, router.OriginId, notification));
            var properties = new BasicProperties { ContentType = "application/json", Type = BrokerEnvelopeCodec.MessageType,
                MessageId = notification.EventId.ToString("D"), Persistent = false };
            await channel.BasicPublishAsync(settings.Exchange, BrokerEnvelopeCodec.MessageType, mandatory: true,
                basicProperties: properties, body: bytes, cancellationToken: deadline.Token);
            router.Confirmed();
            logger.LogInformation("Broker publication confirmed {EventId}", notification.EventId);
        }
    }
}
