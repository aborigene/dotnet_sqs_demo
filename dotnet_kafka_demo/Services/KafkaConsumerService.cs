using Confluent.Kafka;

namespace KafkaDemo.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly string _topic;

    public KafkaConsumerService(IConfiguration configuration, ILogger<KafkaConsumerService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];
        _topic = _configuration["Kafka:Topic"] ?? "demo-topic";
        var groupId = _configuration["Kafka:GroupId"] ?? "kafka-demo-consumer-group";
        
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            throw new InvalidOperationException("Kafka Bootstrap Servers are not configured");
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _logger.LogInformation(
            "Kafka consumer initialized with bootstrap servers: {BootstrapServers}, group: {GroupId}",
            bootstrapServers,
            groupId
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);
        _logger.LogInformation("Kafka consumer subscribed to topic: {Topic}", _topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    
                    if (consumeResult != null)
                    {
                        _logger.LogInformation(
                            "Message received from topic {Topic}, partition {Partition}, offset {Offset}: Key={Key}, Value={Value}",
                            consumeResult.Topic,
                            consumeResult.Partition.Value,
                            consumeResult.Offset.Value,
                            consumeResult.Message.Key,
                            consumeResult.Message.Value
                        );
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }

                await Task.Delay(100, stoppingToken); // Small delay to prevent tight loop
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka consumer service is stopping");
        }
        finally
        {
            _consumer.Close();
            _consumer.Dispose();
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
