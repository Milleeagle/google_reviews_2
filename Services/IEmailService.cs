using google_reviews.Models;

namespace google_reviews.Services
{
    public interface IEmailService
    {
        Task<bool> SendReviewReportEmailAsync(string recipientEmail, ReviewMonitorReport report, string monitorName);
        Task<bool> SendTestEmailAsync(string recipientEmail);
        Task<bool> IsConfiguredAsync();
    }
}