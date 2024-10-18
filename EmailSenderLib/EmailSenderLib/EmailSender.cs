using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Threading.Tasks;

public class EmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IConfiguration config, ILogger<EmailSender> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("Recipient email address cannot be null or empty.", nameof(to));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Email subject cannot be null or empty.", nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Email body cannot be null or empty.", nameof(body));

        var maxRetries = _config.GetValue<int>("EmailSettings:MaxRetries", 3);
        var attempt = 0;
        bool emailSent = false;

        while (!emailSent && attempt < maxRetries)
        {
            try
            {
                var emailMessage = CreateEmailMessage(to, subject, body);

                using (var client = new SmtpClient())
                {
                    await ConnectSmtpClientAsync(client);
                    await client.SendAsync(emailMessage);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation("Email sent to {To} at {DateTime}", to, DateTime.UtcNow);
                emailSent = true;
            }
            catch (Exception ex)
            {
                attempt++;
                _logger.LogError(ex, "Attempt {Attempt}: Failed to send email to {To}. Error: {Message}", attempt, to, ex.Message);

                if (attempt == maxRetries)
                {
                    _logger.LogError("Max retry limit reached. Email not sent to {To}", to);
                    throw new ApplicationException($"Failed to send email after {maxRetries} attempts", ex);
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
            }
        }
    }

    private MimeMessage CreateEmailMessage(string to, string subject, string body)
    {
        var emailMessage = new MimeMessage();

        var fromName = _config.GetValue<string>("EmailSettings:FromName");
        var fromEmail = _config.GetValue<string>("EmailSettings:FromEmail");

        if (string.IsNullOrWhiteSpace(fromName) || string.IsNullOrWhiteSpace(fromEmail))
        {
            _logger.LogError("FromName or FromEmail is missing in the configuration");
            throw new InvalidOperationException("Sender email configuration is invalid");
        }

        emailMessage.From.Add(new MailboxAddress(fromName, fromEmail));

        try
        {
            emailMessage.To.Add(MailboxAddress.Parse(to));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid recipient email address: {To}", to);
            throw new ArgumentException("Invalid recipient email address", nameof(to), ex);
        }

        emailMessage.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = body };
        emailMessage.Body = bodyBuilder.ToMessageBody();

        return emailMessage;
    }

    private async Task ConnectSmtpClientAsync(SmtpClient client)
    {
        var smtpServer = _config.GetValue<string>("EmailSettings:SmtpServer");
        var smtpPort = _config.GetValue<int>("EmailSettings:SmtpPort");
        var username = _config.GetValue<string>("EmailSettings:Username");
        var password = _config.GetValue<string>("EmailSettings:Password");

        if (string.IsNullOrWhiteSpace(smtpServer) || smtpPort == 0 ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogError("SMTP configuration is incomplete");
            throw new InvalidOperationException("SMTP configuration is incomplete");
        }

        try
        {
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.Auto);
            await client.AuthenticateAsync(username, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SMTP server");
            throw new InvalidOperationException("Failed to connect to SMTP server", ex);
        }
    }
}