using google_reviews.Models;

namespace google_reviews.Services
{
    public interface IScheduledMonitorService
    {
        Task ProcessScheduledMonitorsAsync();
        Task<ScheduledReviewMonitor> CreateMonitorAsync(ScheduledReviewMonitor monitor, List<string> companyIds);
        Task<ScheduledReviewMonitor?> UpdateMonitorAsync(string id, ScheduledReviewMonitor monitor, List<string> companyIds);
        Task<bool> DeleteMonitorAsync(string id);
        Task<bool> ToggleMonitorActiveAsync(string id);
        Task<List<ScheduledReviewMonitor>> GetAllMonitorsAsync();
        Task<ScheduledReviewMonitor?> GetMonitorAsync(string id);
        Task<List<ScheduledMonitorExecution>> GetExecutionHistoryAsync(string monitorId, int limit = 50);
        DateTime CalculateNextRunTime(ScheduledReviewMonitor monitor);
    }
}