using Microsoft.EntityFrameworkCore;
using google_reviews.Data;
using google_reviews.Models;

namespace google_reviews.Services
{
    public class ScheduledMonitorService : IScheduledMonitorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGooglePlacesService _googlePlacesService;
        private readonly IEmailService _emailService;
        private readonly ILogger<ScheduledMonitorService> _logger;

        public ScheduledMonitorService(
            ApplicationDbContext context,
            IGooglePlacesService googlePlacesService,
            IEmailService emailService,
            ILogger<ScheduledMonitorService> logger)
        {
            _context = context;
            _googlePlacesService = googlePlacesService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task ProcessScheduledMonitorsAsync()
        {
            var now = DateTime.UtcNow;
            var dueMonitors = await _context.ScheduledReviewMonitors
                .Where(m => m.IsActive && m.NextRunAt <= now)
                .Include(m => m.Companies)
                .ThenInclude(c => c.Company)
                .ToListAsync();

            _logger.LogInformation($"Found {dueMonitors.Count} scheduled monitors ready to execute");

            foreach (var monitor in dueMonitors)
            {
                await ExecuteMonitorAsync(monitor);
            }
        }

        private async Task ExecuteMonitorAsync(ScheduledReviewMonitor monitor)
        {
            var execution = new ScheduledMonitorExecution
            {
                ScheduledReviewMonitorId = monitor.Id,
                ExecutedAt = DateTime.UtcNow,
                PeriodEnd = DateTime.UtcNow,
                PeriodStart = DateTime.UtcNow.AddDays(-monitor.ReviewPeriodDays)
            };

            try
            {
                _logger.LogInformation($"Executing scheduled monitor: {monitor.Name}");

                // Get companies to check
                List<Company> companiesToCheck;
                if (monitor.IncludeAllCompanies)
                {
                    companiesToCheck = await _context.Companies
                        .Where(c => c.IsActive && !string.IsNullOrEmpty(c.PlaceId))
                        .ToListAsync();
                }
                else
                {
                    companiesToCheck = monitor.Companies
                        .Where(mc => mc.Company != null && mc.Company.IsActive && !string.IsNullOrEmpty(mc.Company.PlaceId))
                        .Select(mc => mc.Company!)
                        .ToList();
                }

                execution.CompaniesChecked = companiesToCheck.Count;

                var reportData = new List<CompanyReviewReport>();

                foreach (var company in companiesToCheck)
                {
                    try
                    {
                        var reviewData = await _googlePlacesService.GetFilteredReviewsAsync(
                            company, execution.PeriodStart, execution.PeriodEnd, null, monitor.MaxRating);

                        if (reviewData?.Reviews?.Any() == true)
                        {
                            var badReviews = reviewData.Reviews
                                .Where(r => r.Rating <= monitor.MaxRating && r.Time >= execution.PeriodStart && r.Time <= execution.PeriodEnd)
                                .OrderBy(r => r.Rating)
                                .ThenByDescending(r => r.Time)
                                .ToList();

                            if (badReviews.Any())
                            {
                                reportData.Add(new CompanyReviewReport
                                {
                                    Company = company,
                                    BadReviews = badReviews,
                                    AverageRating = badReviews.Average(r => r.Rating),
                                    TotalBadReviews = badReviews.Count
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error checking reviews for company {company.Name} in monitor {monitor.Name}");
                    }
                }

                // Create report
                var report = new ReviewMonitorReport
                {
                    FromDate = execution.PeriodStart,
                    ToDate = execution.PeriodEnd,
                    MaxRating = monitor.MaxRating,
                    CompanyReports = reportData.OrderBy(r => r.AverageRating).ToList(),
                    GeneratedAt = DateTime.UtcNow,
                    TotalCompaniesChecked = execution.CompaniesChecked,
                    CompaniesWithIssues = reportData.Count,
                    TotalBadReviews = reportData.Sum(r => r.TotalBadReviews)
                };

                execution.CompaniesWithIssues = report.CompaniesWithIssues;
                execution.TotalBadReviews = report.TotalBadReviews;

                // Send email report
                var emailSent = await _emailService.SendReviewReportEmailAsync(
                    monitor.EmailAddress, report, monitor.Name);

                execution.EmailSent = emailSent;
                if (!emailSent)
                {
                    execution.EmailError = "Failed to send email report";
                    execution.Status = ExecutionStatus.PartialSuccess;
                }

                // Update monitor's last run and next run times
                monitor.LastRunAt = DateTime.UtcNow;
                monitor.NextRunAt = CalculateNextRunTime(monitor);

                _logger.LogInformation($"Monitor {monitor.Name} executed successfully. Found {report.CompaniesWithIssues} companies with issues, {report.TotalBadReviews} bad reviews");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing scheduled monitor: {monitor.Name}");
                execution.Status = ExecutionStatus.Failed;
                execution.EmailError = ex.Message;
                
                // Still update next run time so it doesn't get stuck
                monitor.NextRunAt = CalculateNextRunTime(monitor);
            }

            // Save execution record
            _context.ScheduledMonitorExecutions.Add(execution);
            await _context.SaveChangesAsync();
        }

        public async Task<ScheduledReviewMonitor> CreateMonitorAsync(ScheduledReviewMonitor monitor, List<string> companyIds)
        {
            monitor.NextRunAt = CalculateNextRunTime(monitor);
            
            _context.ScheduledReviewMonitors.Add(monitor);

            if (!monitor.IncludeAllCompanies && companyIds.Any())
            {
                foreach (var companyId in companyIds)
                {
                    _context.ScheduledMonitorCompanies.Add(new ScheduledMonitorCompany
                    {
                        ScheduledReviewMonitorId = monitor.Id,
                        CompanyId = companyId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return monitor;
        }

        public async Task<ScheduledReviewMonitor?> UpdateMonitorAsync(string id, ScheduledReviewMonitor monitor, List<string> companyIds)
        {
            var existingMonitor = await _context.ScheduledReviewMonitors
                .Include(m => m.Companies)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existingMonitor == null) return null;

            // Update monitor properties
            existingMonitor.Name = monitor.Name;
            existingMonitor.Description = monitor.Description;
            existingMonitor.EmailAddress = monitor.EmailAddress;
            existingMonitor.ScheduleType = monitor.ScheduleType;
            existingMonitor.ScheduleTime = monitor.ScheduleTime;
            existingMonitor.DayOfWeek = monitor.DayOfWeek;
            existingMonitor.DayOfMonth = monitor.DayOfMonth;
            existingMonitor.MaxRating = monitor.MaxRating;
            existingMonitor.ReviewPeriodDays = monitor.ReviewPeriodDays;
            existingMonitor.IsActive = monitor.IsActive;
            existingMonitor.IncludeAllCompanies = monitor.IncludeAllCompanies;
            existingMonitor.NextRunAt = CalculateNextRunTime(existingMonitor);

            // Update company associations
            _context.ScheduledMonitorCompanies.RemoveRange(existingMonitor.Companies);

            if (!monitor.IncludeAllCompanies && companyIds.Any())
            {
                foreach (var companyId in companyIds)
                {
                    _context.ScheduledMonitorCompanies.Add(new ScheduledMonitorCompany
                    {
                        ScheduledReviewMonitorId = existingMonitor.Id,
                        CompanyId = companyId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return existingMonitor;
        }

        public async Task<bool> DeleteMonitorAsync(string id)
        {
            var monitor = await _context.ScheduledReviewMonitors.FindAsync(id);
            if (monitor == null) return false;

            _context.ScheduledReviewMonitors.Remove(monitor);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleMonitorActiveAsync(string id)
        {
            var monitor = await _context.ScheduledReviewMonitors.FindAsync(id);
            if (monitor == null) return false;

            monitor.IsActive = !monitor.IsActive;
            if (monitor.IsActive)
            {
                monitor.NextRunAt = CalculateNextRunTime(monitor);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ScheduledReviewMonitor>> GetAllMonitorsAsync()
        {
            return await _context.ScheduledReviewMonitors
                .Include(m => m.Companies)
                .ThenInclude(c => c.Company)
                .OrderBy(m => m.Name)
                .ToListAsync();
        }

        public async Task<ScheduledReviewMonitor?> GetMonitorAsync(string id)
        {
            return await _context.ScheduledReviewMonitors
                .Include(m => m.Companies)
                .ThenInclude(c => c.Company)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<ScheduledMonitorExecution>> GetExecutionHistoryAsync(string monitorId, int limit = 50)
        {
            return await _context.ScheduledMonitorExecutions
                .Where(e => e.ScheduledReviewMonitorId == monitorId)
                .OrderByDescending(e => e.ExecutedAt)
                .Take(limit)
                .ToListAsync();
        }

        public DateTime CalculateNextRunTime(ScheduledReviewMonitor monitor)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;
            var scheduleTime = monitor.ScheduleTime;

            switch (monitor.ScheduleType)
            {
                case ScheduleType.Daily:
                    var nextDaily = today.Add(scheduleTime);
                    return nextDaily <= now ? nextDaily.AddDays(1) : nextDaily;

                case ScheduleType.Weekly:
                    if (!monitor.DayOfWeek.HasValue)
                        throw new InvalidOperationException("DayOfWeek must be set for weekly schedules");

                    var daysUntilTarget = ((int)monitor.DayOfWeek.Value - (int)today.DayOfWeek + 7) % 7;
                    if (daysUntilTarget == 0 && today.Add(scheduleTime) <= now)
                        daysUntilTarget = 7;

                    return today.AddDays(daysUntilTarget).Add(scheduleTime);

                case ScheduleType.Monthly:
                    if (!monitor.DayOfMonth.HasValue)
                        throw new InvalidOperationException("DayOfMonth must be set for monthly schedules");

                    var targetDay = Math.Min(monitor.DayOfMonth.Value, DateTime.DaysInMonth(today.Year, today.Month));
                    var nextMonthly = new DateTime(today.Year, today.Month, targetDay).Add(scheduleTime);
                    
                    if (nextMonthly <= now)
                    {
                        var nextMonth = today.AddMonths(1);
                        targetDay = Math.Min(monitor.DayOfMonth.Value, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
                        nextMonthly = new DateTime(nextMonth.Year, nextMonth.Month, targetDay).Add(scheduleTime);
                    }
                    
                    return nextMonthly;

                default:
                    throw new ArgumentException($"Unknown schedule type: {monitor.ScheduleType}");
            }
        }
    }
}