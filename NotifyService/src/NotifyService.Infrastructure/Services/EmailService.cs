using System.Net;
using System.Net.Mail;

namespace NotifyService.Infrastructure.Services;

public interface IEmailService
{
    Task SendNotificationEmailAsync(string email, string title, string message, string type);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendNotificationEmailAsync(string email, string title, string message, string type)
    {
        try
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = _configuration.GetValue<int>("Email:SmtpPort");
            var smtpUsername = _configuration["Email:Username"];
            var smtpPassword = _configuration["Email:Password"];
            var fromEmail = _configuration["Email:FromEmail"];

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, "Notification Service"),
                Subject = $"[{type.ToUpper()}] {title}",
                Body = GenerateEmailBody(title, message, type),
                IsBodyHtml = true
            };

            mailMessage.To.Add(email);

            await smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
            throw;
        }
    }

    private string GenerateEmailBody(string title, string message, string type)
    {
        return $@"
            <html>
            <body style='font-family: Arial, sans-serif; margin: 20px;'>
                <div style='border: 1px solid #ddd; border-radius: 5px; padding: 20px; max-width: 600px;'>
                    <h2 style='color: #333; margin-top: 0;'>{title}</h2>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 3px; margin: 15px 0;'>
                        <p style='margin: 0; color: #555;'>{message}</p>
                    </div>
                    <p style='color: #666; font-size: 12px; margin-bottom: 0;'>
                        Notification Type: <strong>{type}</strong><br>
                        Sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                    </p>
                </div>
            </body>
            </html>";
    }
}
