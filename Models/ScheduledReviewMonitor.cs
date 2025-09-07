using System.ComponentModel.DataAnnotations;

namespace google_reviews.Models
{
    public class ScheduledReviewMonitor
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [Display(Name = "Monitor Name")]
        public string Name { get; set; } = "";
        
        [Display(Name = "Description")]
        public string? Description { get; set; }
        
        [Required]
        [Display(Name = "Email Address")]
        [EmailAddress]
        public string EmailAddress { get; set; } = "";
        
        [Required]
        [Display(Name = "Schedule Type")]
        public ScheduleType ScheduleType { get; set; } = ScheduleType.Daily;
        
        [Display(Name = "Schedule Time")]
        public TimeSpan ScheduleTime { get; set; } = new TimeSpan(9, 0, 0); // 9:00 AM
        
        [Display(Name = "Day of Week")]
        public DayOfWeek? DayOfWeek { get; set; } // For weekly schedules
        
        [Display(Name = "Day of Month")]
        public int? DayOfMonth { get; set; } // For monthly schedules
        
        [Required]
        [Range(1, 5)]
        [Display(Name = "Maximum Rating (Bad Reviews)")]
        public int MaxRating { get; set; } = 3;
        
        [Required]
        [Range(1, 365)]
        [Display(Name = "Review Period (Days)")]
        public int ReviewPeriodDays { get; set; } = 7;
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Include All Companies")]
        public bool IncludeAllCompanies { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastRunAt { get; set; }
        public DateTime NextRunAt { get; set; }
        
        // Navigation properties
        public virtual List<ScheduledMonitorCompany> Companies { get; set; } = new();
        public virtual List<ScheduledMonitorExecution> Executions { get; set; } = new();
    }

    public class ScheduledMonitorCompany
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string ScheduledReviewMonitorId { get; set; } = "";
        
        [Required]
        public string CompanyId { get; set; } = "";
        
        // Navigation properties
        public virtual ScheduledReviewMonitor? ScheduledReviewMonitor { get; set; }
        public virtual Company? Company { get; set; }
    }

    public class ScheduledMonitorExecution
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        public string ScheduledReviewMonitorId { get; set; } = "";
        
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int CompaniesChecked { get; set; }
        public int CompaniesWithIssues { get; set; }
        public int TotalBadReviews { get; set; }
        public bool EmailSent { get; set; }
        public string? EmailError { get; set; }
        public ExecutionStatus Status { get; set; } = ExecutionStatus.Success;
        
        // Navigation property
        public virtual ScheduledReviewMonitor? ScheduledReviewMonitor { get; set; }
    }

    public enum ScheduleType
    {
        [Display(Name = "Daily")]
        Daily = 0,
        
        [Display(Name = "Weekly")]
        Weekly = 1,
        
        [Display(Name = "Monthly")]
        Monthly = 2
    }

    public enum ExecutionStatus
    {
        [Display(Name = "Success")]
        Success = 0,
        
        [Display(Name = "Failed")]
        Failed = 1,
        
        [Display(Name = "Partial Success")]
        PartialSuccess = 2
    }
}