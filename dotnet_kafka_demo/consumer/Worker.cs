using Confluent.Kafka;
using System.Text.Json;

namespace KafkaConsumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConsumer<string, string> _consumer;
    private readonly string _topic;
    private readonly int _commitBatchSize;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Get configuration values
        var bootstrapServers = _configuration["Kafka:BootstrapServers"] 
            ?? throw new InvalidOperationException("Kafka Bootstrap Servers are not configured");
        
        _topic = _configuration["Kafka:Topic"] 
            ?? throw new InvalidOperationException("Kafka Topic is not configured");
        
        var groupId = _configuration["Kafka:GroupId"] ?? "kafka-demo-consumer-group";
        
        _commitBatchSize = _configuration.GetValue<int>("Kafka:CommitBatchSize", 10);
        
        // Initialize Kafka consumer
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false // Manual commit for better control
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        
        _logger.LogInformation("Kafka Consumer initialized with Bootstrap Servers: {BootstrapServers}", bootstrapServers);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Consumer Worker started at: {time}", DateTimeOffset.Now);

        _consumer.Subscribe(_topic);
        _logger.LogInformation("Subscribed to topic: {Topic}", _topic);

        var messageCount = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = await Task.Run(() => _consumer.Consume(stoppingToken), stoppingToken);
                
                if (consumeResult != null)
                {
                    await ProcessMessageAsync(consumeResult, stoppingToken);
                    
                    messageCount++;
                    
                    // Commit offsets in batches
                    if (messageCount >= _commitBatchSize)
                    {
                        _consumer.Commit(consumeResult);
                        _logger.LogDebug("Committed offset for batch of {MessageCount} messages", messageCount);
                        messageCount = 0;
                    }
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error occurred while consuming messages from Kafka");
                
                // Wait before retrying to avoid tight loop on persistent errors
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consumer operation cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in consumer loop");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        
        _logger.LogInformation("Kafka Consumer Worker stopped at: {time}", DateTimeOffset.Now);
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> consumeResult, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing message: Topic={Topic}, Partition={Partition}, Offset={Offset}",
                consumeResult.Topic,
                consumeResult.Partition.Value,
                consumeResult.Offset.Value
            );
            _logger.LogInformation("Message Body: {Body}", consumeResult.Message.Value);

            // Parse the message body
            var messageData = JsonSerializer.Deserialize<MessageData>(consumeResult.Message.Value);
            
            if (messageData != null)
            {
                _logger.LogInformation(
                    "Parsed Message - ID: {Id}, Timestamp: {Timestamp}", 
                    messageData.Id, 
                    messageData.Timestamp
                );
                
                // TODO: Add your business logic here
                // For example: Save to database, call external API, etc.
                await ProcessBusinessLogicAsync(messageData);
            }
            
            _logger.LogInformation(
                "Successfully processed message from partition {Partition}, offset {Offset}",
                consumeResult.Partition.Value,
                consumeResult.Offset.Value
            );
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex, 
                "Failed to parse message body for message at partition {Partition}, offset {Offset}",
                consumeResult.Partition.Value,
                consumeResult.Offset.Value
            );
            
            // Skip invalid messages
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Failed to process message at partition {Partition}, offset {Offset}",
                consumeResult.Partition.Value,
                consumeResult.Offset.Value
            );
            
            // Consider implementing retry logic or dead letter queue
            throw;
        }
    }

    private async Task ProcessBusinessLogicAsync(MessageData messageData)
    {
        // Simulate processing time
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        
        _logger.LogInformation("Business logic processed for ID: {Id}", messageData.Id);
        
        // Add your actual business logic here
        // Examples:
        // - Store in database
        // - Send email notification
        // - Call external API
        // - Transform and forward to another service
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Consumer Worker is stopping...");
        
        try
        {
            _consumer.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing Kafka consumer");
        }
        
        await base.StopAsync(stoppingToken);
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}

public class MessageData
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
