using Microsoft.AspNetCore.Mvc;
using KafkaDemo.Models;
using KafkaDemo.Services;

namespace KafkaDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly IKafkaService _kafkaService;
    private readonly ILogger<MessageController> _logger;

    public MessageController(IKafkaService kafkaService, ILogger<MessageController> logger)
    {
        _kafkaService = kafkaService;
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
            var messageId = await _kafkaService.SendMessageAsync(request.Id);
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
