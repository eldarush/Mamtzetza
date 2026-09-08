using Google.Protobuf;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OmegaFireflyComponent;

public class OmegaFireflyWorker : BackgroundService
{
    private readonly IFireflyTransformer _transformer;
    private readonly ComponentOptions _options;
    private readonly ILogger<OmegaFireflyWorker> _logger;

    public OmegaFireflyWorker(
        IFireflyTransformer transformer,
        IOptions<ComponentOptions> options,
        ILogger<OmegaFireflyWorker> logger)
    {
        _transformer = transformer;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting OmegaFireflyWorker with RabbitMQ host: {Host}:{Port}", _options.RabbitMqHost, _options.RabbitMqPort);

        var factory = new ConnectionFactory
        {
            HostName = _options.RabbitMqHost,
            Port = _options.RabbitMqPort,
            UserName = _options.RabbitMqUsername,
            Password = _options.RabbitMqPassword
        };

        IConnection? connection = null;
        IChannel? channel = null;

        // Connect retry loop (wait for RabbitMQ to become ready)
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Attempting to connect to RabbitMQ at {Host}:{Port}...", _options.RabbitMqHost, _options.RabbitMqPort);
                connection = await factory.CreateConnectionAsync(stoppingToken);
                channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                _logger.LogInformation("Connected successfully to RabbitMQ!");
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("Could not connect to RabbitMQ: {Message}. Retrying in 2 seconds...", ex.Message);
                await Task.Delay(2000, stoppingToken);
            }
        }

        if (connection == null || channel == null || stoppingToken.IsCancellationRequested)
            return;

        // Declare exchanges and queues
        await channel.ExchangeDeclareAsync(
            exchange: _options.InputExchange,
            type: ExchangeType.Direct,
            durable: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: _options.InputQueue,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.QueueBindAsync(
            queue: _options.InputQueue,
            exchange: _options.InputExchange,
            routingKey: _options.InputRoutingKey,
            cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.OutputExchange,
            type: ExchangeType.Direct,
            durable: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            try
            {
                var inputSoldier = OmegaSolider.Messages.OmegaSolider.Parser.ParseFrom(ea.Body.ToArray());
                _logger.LogInformation("Received message: SoldierId={SoldierId}", inputSoldier.SoldierId);

                var outputFirefly = await _transformer.TransformAsync(inputSoldier, stoppingToken);
                var outputBytes = outputFirefly.ToByteArray();

                var publishProps = new BasicProperties
                {
                    ContentType = "application/x-protobuf"
                };

                await channel.BasicPublishAsync(
                    exchange: _options.OutputExchange,
                    routingKey: _options.OutputRoutingKey,
                    mandatory: false,
                    basicProperties: publishProps,
                    body: outputBytes,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("Published transformed FireflyExpert: SoldierId={SoldierId}, Glow={Glow}",
                    outputFirefly.SoldierId, outputFirefly.GlowIntensity);

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing incoming message.");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _options.InputQueue,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("OmegaFireflyWorker is actively consuming from queue {Queue}...", _options.InputQueue);

        // Keep running until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Worker shutdown requested.");
        }
        finally
        {
            if (channel != null)
                await channel.CloseAsync();
            if (connection != null)
                await connection.CloseAsync();
        }
    }
}
