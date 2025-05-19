// src/CampusBites.Application/Common/Interfaces/IEmailSender.cs
using System.Threading.Tasks;

namespace CampusBites.Application.Common.Interfaces;

public interface IEmailSender
{
    /// <summary>
    /// Sends an email.
    /// </summary>
    /// <param name="toEmail">Recipient's email address.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="htmlMessage">Email body (HTML format).</param>
    Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
}