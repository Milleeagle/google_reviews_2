using google_reviews.Models;

namespace google_reviews.Services
{
    public interface IEmailService
    {
        Task<bool> SendReviewReportEmailAsync(string recipientEmail, ReviewMonitorReport report, string monitorName);
        Task<bool> SendReviewReportEmailWithExcelAsync(string recipientEmail, ReviewMonitorReport report, string monitorName, byte[] excelData, string excelFileName);
        Task<bool> SendBatchReviewNotificationAsync(string recipientEmail, List<CompanyReviewData> reviewData, List<string> results, int successCount, int failCount);
        Task<bool> SendTestEmailAsync(string recipientEmail);
        Task<bool> IsConfiguredAsync();
    }
}