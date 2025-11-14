using Confluent.Kafka;
using System.Text.Json;

namespace KafkaDemo.Services;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _topic;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];
        _topic = _configuration["Kafka:Topic"] ?? "demo-topic";
        
        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            throw new InvalidOperationException("Kafka Bootstrap Servers are not configured");
        }

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = "kafka-demo-producer"
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger.LogInformation("Kafka producer initialized with bootstrap servers: {BootstrapServers}", bootstrapServers);
    }

    public async Task<string> SendMessageAsync(string id)
    {
        var messageBody = JsonSerializer.Serialize(new
        {
            Id = id,
            Timestamp = DateTime.UtcNow
        });

        try
        {
            var message = new Message<string, string>
            {
                Key = id,
                Value = messageBody
            };

            _logger.LogInformation("Sending message to Kafka topic: {Topic}", _topic);
            
            var deliveryResult = await _producer.ProduceAsync(_topic, message);
            
            _logger.LogInformation(
                "Message delivered to topic {Topic}, partition {Partition}, offset {Offset}",
                deliveryResult.Topic,
                deliveryResult.Partition.Value,
                deliveryResult.Offset.Value
            );

            return $"{deliveryResult.Topic}-{deliveryResult.Partition.Value}-{deliveryResult.Offset.Value}";
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Error producing message to Kafka");
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
