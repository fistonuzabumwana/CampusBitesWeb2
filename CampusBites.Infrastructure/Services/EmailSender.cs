// src/CampusBites.Infrastructure/Services/EmailSender.cs
using CampusBites.Application.Common.Interfaces;
using CampusBites.Application.Common.Options; // For MailSettings
using MailKit.Net.Smtp;
using MailKit.Security; // For SecureSocketOptions
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // For IOptions
using MimeKit; // For MimeMessage etc.
using System;
using System.Threading.Tasks;

namespace CampusBites.Infrastructure.Services;

public class EmailSender : IEmailSender
{
    private readonly MailSettings _mailSettings;
    private readonly ILogger<EmailSender> _logger;

    // Inject MailSettings using IOptions
    public EmailSender(IOptions<MailSettings> mailSettings, ILogger<EmailSender> logger)
    {
        _mailSettings = mailSettings.Value; // Get the settings instance
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
    {
        if (string.IsNullOrEmpty(_mailSettings.SmtpHost) || string.IsNullOrEmpty(_mailSettings.FromEmail))
        {
            _logger.LogError("Email settings (Host, FromEmail) are not configured.");
            // Decide if you should throw or just log and return
            return; // Or throw new InvalidOperationException("Email settings not configured.");
        }

        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_mailSettings.DisplayName ?? "CampusBites", _mailSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlMessage };
            email.Body = builder.ToMessageBody();

            _logger.LogInformation("Attempting to send email to {ToEmail} via {SmtpHost}:{SmtpPort}", toEmail, _mailSettings.SmtpHost, _mailSettings.SmtpPort);

            using var smtp = new SmtpClient();

            // Determine connection options based on settings
            SecureSocketOptions socketOptions = SecureSocketOptions.Auto;
            if (_mailSettings.UseSsl) socketOptions = SecureSocketOptions.SslOnConnect;
            else if (_mailSettings.UseStartTls) socketOptions = SecureSocketOptions.StartTls;

            await smtp.ConnectAsync(_mailSettings.SmtpHost, _mailSettings.SmtpPort, socketOptions);
            _logger.LogDebug("Connected to SMTP server.");

            // Authenticate only if User/Pass are provided
            if (!string.IsNullOrEmpty(_mailSettings.SmtpPass))
            {
                // Use SmtpUser if provided, otherwise fallback to FromEmail
                string smtpUser = !string.IsNullOrEmpty(_mailSettings.SmtpUser) ? _mailSettings.SmtpUser : _mailSettings.FromEmail;
                await smtp.AuthenticateAsync(smtpUser, _mailSettings.SmtpPass);
                _logger.LogDebug("Authenticated with SMTP server.");
            }

            await smtp.SendAsync(email);
            _logger.LogInformation("Email sent successfully to {ToEmail}.", toEmail);

            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            // Depending on requirements, you might re-throw or just log
            // throw; // Uncomment if email failure should stop the process
        }
    }
}