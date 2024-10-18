using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

[Route("api/[controller]")]
[ApiController]
public class EmailController : ControllerBase
{
    private readonly EmailSender _emailSender;
    private readonly ILogger<EmailController> _logger;

    public EmailController(EmailSender emailSender, ILogger<EmailController> logger)
    {
        _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequest emailRequest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _emailSender.SendEmailAsync(emailRequest.To, emailRequest.Subject, emailRequest.Body);
            _logger.LogInformation("Email sent successfully to {Recipient}", emailRequest.To);
            return Ok("Email has been sent successfully");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid email request");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending email to {Recipient}", emailRequest.To);
            return StatusCode(500, "An error occurred while sending the email. Please try again later.");
        }
    }
}

public class EmailRequest
{
    [Required(ErrorMessage = "Recipient email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string To { get; set; }

    [Required(ErrorMessage = "Subject is required")]
    public string Subject { get; set; }

    [Required(ErrorMessage = "Body is required")]
    public string Body { get; set; }
}