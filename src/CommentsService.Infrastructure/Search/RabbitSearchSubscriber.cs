using CommentsService.Infrastructure.Broker;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
namespace CommentsService.Infrastructure.Search;
public sealed class RabbitSearchSubscriber(BrokerSettings broker, SearchSettings settings, SearchSignals signals, ILogger<RabbitSearchSubscriber> logger)
{
    public async Task RunSessionAsync(CancellationToken token)
    {
        var password = Environment.GetEnvironmentVariable("BROKER_PASSWORD");
        if (string.IsNullOrEmpty(password)) throw new InvalidOperationException("Broker secret unavailable.");
        var factory = new ConnectionFactory { HostName=broker.Host, Port=broker.Port, UserName=broker.Username, Password=password,
            AutomaticRecoveryEnabled=false, TopologyRecoveryEnabled=false, MaxInboundMessageBodySize=BrokerEnvelopeCodec.MaximumBytes,
            RequestedConnectionTimeout=TimeSpan.FromSeconds(broker.OperationTimeoutSeconds), ContinuationTimeout=TimeSpan.FromSeconds(broker.OperationTimeoutSeconds),
            HandshakeContinuationTimeout=TimeSpan.FromSeconds(broker.OperationTimeoutSeconds), RequestedHeartbeat=TimeSpan.FromSeconds(5), ConsumerDispatchConcurrency=1 };
        await using var connection = await factory.CreateConnectionAsync(token);
        await using var channel = await connection.CreateChannelAsync(cancellationToken:token);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.ConnectionShutdownAsync += (_,_) => { closed.TrySetResult(); return Task.CompletedTask; };
        channel.ChannelShutdownAsync += (_,_) => { closed.TrySetResult(); return Task.CompletedTask; };
        await channel.ExchangeDeclareAsync(broker.Exchange, ExchangeType.Topic, false, false, cancellationToken:token);
        var queue = await channel.QueueDeclareAsync("",false,true,true,new Dictionary<string,object?> { ["x-max-length"]=settings.Capacity,
            ["x-max-length-bytes"]=settings.Capacity*BrokerEnvelopeCodec.MaximumBytes, ["x-message-ttl"]=60000, ["x-overflow"]="drop-head" },cancellationToken:token);
        await channel.QueueBindAsync(queue.QueueName,broker.Exchange,BrokerEnvelopeCodec.MessageType,cancellationToken:token);
        await channel.BasicQosAsync(0,16,false,token);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.UnregisteredAsync += (_,_) => { closed.TrySetResult(); return Task.CompletedTask; };
        consumer.ReceivedAsync += async (_,message) => {
            var value = message.BasicProperties.Type==BrokerEnvelopeCodec.MessageType ? BrokerEnvelopeCodec.Decode(message.Body) : null;
            if (value is not null) { signals.Notify(value.Event.CommentId); logger.LogInformation("Search broker signal received {EventId}",value.Event.EventId); }
            await channel.BasicAckAsync(message.DeliveryTag,false,token);
        };
        await channel.BasicConsumeAsync(queue.QueueName,false,consumer,token);
        await closed.Task.WaitAsync(token);
    }
}
