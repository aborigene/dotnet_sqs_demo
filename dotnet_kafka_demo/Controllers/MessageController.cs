using Microsoft.AspNetCore.Mvc;
using KafkaDemo.Models;
using KafkaDemo.Services;

namespace KafkaDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IKafkaProducerService _kafkaProducerService;
    private readonly ILogger<MessageController> _logger;

    public MessageController(IKafkaProducerService kafkaProducerService, ILogger<MessageController> logger)
    {
        _kafkaProducerService = kafkaProducerService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] MessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            return BadRequest(new { error = "ID is required" });
        }

        try
        {
            var messageId = await _kafkaProducerService.SendMessageAsync(request.Id);
            _logger.LogInformation("Message sent to Kafka with ID: {MessageId}", messageId);
            
            return Ok(new 
            { 
                success = true, 
                messageId = messageId,
                sentId = request.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Kafka");
            return StatusCode(500, new { error = "Failed to send message to Kafka", details = ex.Message });
        }
    }
}
