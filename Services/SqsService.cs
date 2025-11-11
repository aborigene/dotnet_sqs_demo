using Amazon.SQS;
using Amazon.SQS.Model;
using System.Text.Json;

namespace SqsDemo.Services;

public class SqsService : ISqsService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SqsService> _logger;

    public SqsService(IConfiguration configuration, ILogger<SqsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Initialize AWS SQS client
        _sqsClient = new AmazonSQSClient();
    }

    public async Task<string> SendMessageAsync(string id)
    {
        var queueUrl = _configuration["AWS:SQS:QueueUrl"];
        
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            throw new InvalidOperationException("SQS Queue URL is not configured");
        }

        var messageBody = JsonSerializer.Serialize(new
        {
            Id = id,
            Timestamp = DateTime.UtcNow
        });

        var sendMessageRequest = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = messageBody
        };

        _logger.LogInformation("Sending message to SQS queue: {QueueUrl}", queueUrl);
        
        var response = await _sqsClient.SendMessageAsync(sendMessageRequest);
        
        return response.MessageId;
    }
}
