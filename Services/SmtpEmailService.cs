using System.Net;
using System.Net.Mail;
using ServicePlatform.Services.Interfaces;

namespace ServicePlatform.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public bool IsConfigured
    {
        get
        {
            try
            {
                var smtpSettings = _config.GetSection("SmtpSettings");
                var host = smtpSettings["Host"];
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];

                var configured = !string.IsNullOrEmpty(host) && 
                                !string.IsNullOrEmpty(username) && 
                                !username.Contains("your-email") && 
                                !string.IsNullOrEmpty(password) && 
                                !password.Contains("your-app-password");
                
                if (!configured)
                {
                    _logger.LogWarning("Email service is NOT configured. Host: {Host}, User: {User}, HasPass: {HasPass}", 
                        host, username, !string.IsNullOrEmpty(password));
                }
                return configured;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email configuration status");
                return false;
            }
        }
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpSettings = _config.GetSection("SmtpSettings");
            var host = smtpSettings["Host"];
            var portStr = smtpSettings["Port"];
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            var fromEmail = smtpSettings["FromEmail"] ?? username;

            var useRealSmtp = IsConfigured;

            using var client = new SmtpClient();

            if (useRealSmtp)
            {
                // REAL SMTP DELIVERY TO INBOX
                int port = 587;
                if (!int.TryParse(portStr, out port)) port = 587;

                client.Host = host!;
                client.Port = port;
                client.Credentials = new NetworkCredential(username, password);
                client.EnableSsl = true;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
            }
            else
            {
                // LOCAL FOLDER DELIVERY (Prevents crashing when credentials are fake)
                var pickupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "SentEmails");
                if (!Directory.Exists(pickupDirectory))
                {
                    Directory.CreateDirectory(pickupDirectory);
                }

                client.Host = "localhost";
                client.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                client.PickupDirectoryLocation = pickupDirectory;
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(useRealSmtp ? (fromEmail ?? "noreply@serviceplatform.com") : "noreply@serviceplatform.com", "ServicePlatform"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(to);

            // Resilience: Simple Retry Logic
            int maxRetries = 3;
            int delayMs = 2000;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await client.SendMailAsync(mailMessage);
                    break; // Success
                }
                catch (Exception ex) when (i < maxRetries - 1)
                {
                    _logger.LogWarning(ex, "Email delivery attempt {Attempt} failed, retrying in {Delay}ms...", i + 1, delayMs);
                    await Task.Delay(delayMs);
                }
            }
            
            if (useRealSmtp)
            {
                _logger.LogInformation("Real email successfully sent over internet to {Email} with subject '{Subject}'", to, subject);
            }
            else
            {
                _logger.LogInformation("Email successfully saved to local folder {PickupDirectory} for {Email} with subject '{Subject}'", client.PickupDirectoryLocation, to, subject);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate email to {Email} with subject '{Subject}'", to, subject);
            throw;
        }
    }
}
