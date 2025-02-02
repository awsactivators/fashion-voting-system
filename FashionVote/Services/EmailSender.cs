using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace FashionVote.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Log email sending (for debugging)
            Console.WriteLine($"📧 FAKE EMAIL SENT: To {email} | Subject: {subject}");
            return Task.CompletedTask;
        }
    }
}
