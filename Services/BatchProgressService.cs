using System.Collections.Concurrent;
using google_reviews.Models;

namespace google_reviews.Services
{
    public class BatchProgressService
    {
        private readonly ConcurrentDictionary<string, (BatchProgressInfo Info, DateTime Timestamp)> _progressData = new();
        private readonly ConcurrentDictionary<string, (ReviewMonitorReport Report, DateTime Timestamp)> _reportCache = new();
        private readonly Timer _cleanupTimer;
        private readonly TimeSpan _expirationTime = TimeSpan.FromHours(2);

        public BatchProgressService()
        {
            // Cleanup old sessions every 15 minutes
            _cleanupTimer = new Timer(CleanupExpiredSessions, null, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
        }

        private void CleanupExpiredSessions(object? state)
        {
            var now = DateTime.UtcNow;
            var expiredProgressKeys = _progressData
                .Where(kvp => now - kvp.Value.Timestamp > _expirationTime)
                .Select(kvp => kvp.Key)
                .ToList();

            var expiredReportKeys = _reportCache
                .Where(kvp => now - kvp.Value.Timestamp > _expirationTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredProgressKeys)
            {
                _progressData.TryRemove(key, out _);
            }

            foreach (var key in expiredReportKeys)
            {
                _reportCache.TryRemove(key, out _);
            }
        }

        public void UpdateProgress(string sessionId, BatchProgressInfo progress)
        {
            _progressData.AddOrUpdate(sessionId, (progress, DateTime.UtcNow), (key, oldValue) => (progress, DateTime.UtcNow));
        }

        public BatchProgressInfo? GetProgress(string sessionId)
        {
            return _progressData.TryGetValue(sessionId, out var data) ? data.Info : null;
        }

        public void ClearProgress(string sessionId)
        {
            _progressData.TryRemove(sessionId, out _);
        }

        public string CreateSession()
        {
            return Guid.NewGuid().ToString();
        }

        public void StoreReport(string sessionId, ReviewMonitorReport report)
        {
            _reportCache.AddOrUpdate(sessionId, (report, DateTime.UtcNow), (key, oldValue) => (report, DateTime.UtcNow));
        }

        public ReviewMonitorReport? GetReport(string sessionId)
        {
            return _reportCache.TryGetValue(sessionId, out var data) ? data.Report : null;
        }

        public void ClearReport(string sessionId)
        {
            _reportCache.TryRemove(sessionId, out _);
        }
    }

    public class BatchProgressInfo
    {
        public int TotalItems { get; set; }
        public int ProcessedItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public string CurrentItem { get; set; } = "";
        public string Status { get; set; } = "";
        public int EstimatedRemainingSeconds { get; set; }
        public bool IsComplete { get; set; }
        public List<string> RecentResults { get; set; } = new List<string>();
        public string RateLimitInfo { get; set; } = "";
        public DateTime StartTime { get; set; }
        public double RequestsPerMinute { get; set; }
        public string? RedirectUrl { get; set; }
    }
}