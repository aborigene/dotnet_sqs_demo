using Microsoft.AspNetCore.Mvc;
using SqsDemo.Models;
using SqsDemo.Services;

namespace SqsDemo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageController : ControllerBase
{
    private readonly ISqsService _sqsService;
    private readonly ILogger<MessageController> _logger;

    public MessageController(ISqsService sqsService, ILogger<MessageController> logger)
    {
        _sqsService = sqsService;
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
            var messageId = await _sqsService.SendMessageAsync(request.Id);
            _logger.LogInformation("Message sent to SQS with ID: {MessageId}", messageId);
            
            return Ok(new 
            { 
                success = true, 
                messageId = messageId,
                sentId = request.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to SQS");
            return StatusCode(500, new { error = "Failed to send message to SQS", details = ex.Message });
        }
    }
}
