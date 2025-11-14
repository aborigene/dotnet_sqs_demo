using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;

namespace SqsConsumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly int _maxNumberOfMessages;
    private readonly int _waitTimeSeconds;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Initialize AWS SQS client
        _sqsClient = new AmazonSQSClient();
        
        // Get configuration values
        _queueUrl = _configuration["AWS:SQS:QueueUrl"] 
            ?? throw new InvalidOperationException("SQS Queue URL is not configured");
        
        _maxNumberOfMessages = _configuration.GetValue<int>("AWS:SQS:MaxNumberOfMessages", 10);
        _waitTimeSeconds = _configuration.GetValue<int>("AWS:SQS:WaitTimeSeconds", 20);
        
        _logger.LogInformation("SQS Consumer initialized with Queue URL: {QueueUrl}", _queueUrl);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SQS Consumer Worker started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while polling messages from SQS");
                
                // Wait before retrying to avoid tight loop on persistent errors
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        
        _logger.LogInformation("SQS Consumer Worker stopped at: {time}", DateTimeOffset.Now);
    }

    private async Task PollMessagesAsync(CancellationToken stoppingToken)
    {
        var receiveMessageRequest = new ReceiveMessageRequest
        {
            QueueUrl = _queueUrl,
            MaxNumberOfMessages = _maxNumberOfMessages,
            WaitTimeSeconds = _waitTimeSeconds, // Long polling
            AttributeNames = new List<string> { "All" },
            MessageAttributeNames = new List<string> { "All" }
        };

        _logger.LogDebug("Polling for messages from queue: {QueueUrl}", _queueUrl);
        
        var response = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);

        if (response.Messages.Count > 0)
        {
            _logger.LogInformation("Received {MessageCount} messages from SQS", response.Messages.Count);

            foreach (var message in response.Messages)
            {
                await ProcessMessageAsync(message, stoppingToken);
            }
        }
        else
        {
            _logger.LogDebug("No messages received from queue");
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Processing message: {MessageId}", message.MessageId);
            _logger.LogInformation("Message Body: {Body}", message.Body);

            // Parse the message body
            var messageData = JsonSerializer.Deserialize<MessageData>(message.Body);
            
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

            // Delete the message from the queue after successful processing
            await DeleteMessageAsync(message.ReceiptHandle, stoppingToken);
            
            _logger.LogInformation("Successfully processed and deleted message: {MessageId}", message.MessageId);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse message body for message: {MessageId}", message.MessageId);
            
            // Optionally: Move to Dead Letter Queue or delete invalid messages
            // For now, we'll delete it to avoid reprocessing
            await DeleteMessageAsync(message.ReceiptHandle, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message: {MessageId}", message.MessageId);
            
            // Message will be returned to the queue for retry after visibility timeout
            // Consider implementing exponential backoff or moving to DLQ after max retries
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

    private async Task DeleteMessageAsync(string receiptHandle, CancellationToken stoppingToken)
    {
        var deleteRequest = new DeleteMessageRequest
        {
            QueueUrl = _queueUrl,
            ReceiptHandle = receiptHandle
        };

        await _sqsClient.DeleteMessageAsync(deleteRequest, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SQS Consumer Worker is stopping...");
        await base.StopAsync(stoppingToken);
    }
}

public class MessageData
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
