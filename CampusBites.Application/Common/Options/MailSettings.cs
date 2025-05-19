// src/CampusBites.Application/Common/Options/MailSettings.cs
namespace CampusBites.Application.Common.Options;

public class MailSettings
{
    public string? DisplayName { get; set; } // Name shown in "From" field
    public string? FromEmail { get; set; }   // Sender email address
    public string? SmtpUser { get; set; }    // SMTP Login Username
    public string? SmtpPass { get; set; }    // SMTP Login Password (Store in User Secrets!)
    public string? SmtpHost { get; set; }    // SMTP Server address
    public int SmtpPort { get; set; }        // SMTP Port (e.g., 587 for TLS)
    public bool UseSsl { get; set; }        // Use SSL/TLS? (True for port 465, false for 587 usually)
    public bool UseStartTls { get; set; }   // Use STARTTLS? (True for port 587 usually)
}