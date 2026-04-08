using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace CityFix.Api.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string body)
        {
            if (!_settings.Enabled)
            {
                _logger.LogInformation("Email sending disabled. Intended recipient: {Email}. Subject: {Subject}. Body: {Body}", toEmail, subject, body);
                return;
            }

            var message = new MailMessage();
            try
            {
                message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = false;
                message.To.Add(toEmail);

                var client = new SmtpClient(_settings.Host, _settings.Port);
                try
                {
                    client.EnableSsl = _settings.UseSsl;
                    client.Credentials = new NetworkCredential(_settings.UserName, _settings.Password);
                    await client.SendMailAsync(message);
                }
                finally
                {
                    client.Dispose();
                }
            }
            finally
            {
                message.Dispose();
            }
        }
    }
}

