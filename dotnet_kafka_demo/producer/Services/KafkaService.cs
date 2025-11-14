using Confluent.Kafka;
using System.Text.Json;

namespace KafkaDemo.Services;

public class KafkaService : IKafkaService
{
    private readonly IProducer<string, string> _producer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaService> _logger;

    public KafkaService(IConfiguration configuration, ILogger<KafkaService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        var bootstrapServers = _configuration["Kafka:BootstrapServers"];
        
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
    }

    public async Task<string> SendMessageAsync(string id)
    {
        var topic = _configuration["Kafka:Topic"];
        
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new InvalidOperationException("Kafka Topic is not configured");
        }

        var messageBody = JsonSerializer.Serialize(new
        {
            Id = id,
            Timestamp = DateTime.UtcNow
        });

        var message = new Message<string, string>
        {
            Key = id,
            Value = messageBody
        };

        _logger.LogInformation("Sending message to Kafka topic: {Topic}", topic);
        
        var result = await _producer.ProduceAsync(topic, message);
        
        return result.TopicPartitionOffset.ToString();
    }
}
