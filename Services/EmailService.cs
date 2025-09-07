using System.Net;
using System.Net.Mail;
using System.Text;
using System.Linq;
using google_reviews.Models;

namespace google_reviews.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendReviewReportEmailAsync(string recipientEmail, ReviewMonitorReport report, string monitorName)
        {
            try
            {
                var emailConfig = _configuration.GetSection("Email");
                var smtpHost = emailConfig["SmtpHost"];
                var smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
                var smtpUsername = emailConfig["SmtpUsername"];
                var smtpPassword = emailConfig["SmtpPassword"];
                var fromEmail = emailConfig["FromEmail"] ?? smtpUsername;
                var fromName = emailConfig["FromName"] ?? "Review Monitor";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogError("Email configuration is missing required settings");
                    return false;
                }

                var subject = report.CompaniesWithIssues > 0 
                    ? $"⚠️ Review Alert: {report.CompaniesWithIssues} companies need attention - {monitorName}"
                    : $"✅ All Clear: No review issues found - {monitorName}";

                var htmlBody = GenerateReportEmailHtml(report, monitorName);

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(recipientEmail);
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;
                message.BodyEncoding = Encoding.UTF8;

                await client.SendMailAsync(message);
                
                _logger.LogInformation($"Review report email sent successfully to {recipientEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send review report email to {recipientEmail}");
                return false;
            }
        }

        public async Task<bool> SendTestEmailAsync(string recipientEmail)
        {
            try
            {
                var emailConfig = _configuration.GetSection("Email");
                var smtpHost = emailConfig["SmtpHost"];
                var smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
                var smtpUsername = emailConfig["SmtpUsername"];
                var smtpPassword = emailConfig["SmtpPassword"];
                var fromEmail = emailConfig["FromEmail"] ?? smtpUsername;
                var fromName = emailConfig["FromName"] ?? "Review Monitor";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    return false;
                }

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(recipientEmail);
                message.Subject = "Test Email - Review Monitor System";
                message.Body = GenerateTestEmailHtml();
                message.IsBodyHtml = true;
                message.BodyEncoding = Encoding.UTF8;

                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send test email to {recipientEmail}");
                return false;
            }
        }

        public Task<bool> IsConfiguredAsync()
        {
            var emailConfig = _configuration.GetSection("Email");
            var smtpHost = emailConfig["SmtpHost"];
            var smtpUsername = emailConfig["SmtpUsername"];
            var smtpPassword = emailConfig["SmtpPassword"];

            var isConfigured = !string.IsNullOrEmpty(smtpHost) && 
                              !string.IsNullOrEmpty(smtpUsername) && 
                              !string.IsNullOrEmpty(smtpPassword);
            
            return Task.FromResult(isConfigured);
        }

        private string GenerateReportEmailHtml(ReviewMonitorReport report, string monitorName)
        {
            var issueSeverity = report.CompaniesWithIssues == 0 ? "success" : 
                               report.CompaniesWithIssues <= 2 ? "warning" : "danger";
            
            var statusIcon = report.CompaniesWithIssues == 0 ? "✅" : "⚠️";
            var statusColor = report.CompaniesWithIssues == 0 ? "#28a745" : "#dc3545";

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Review Monitor Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; background-color: #f8f9fa; }}
        .container {{ max-width: 800px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .header p {{ margin: 10px 0 0 0; opacity: 0.9; }}
        .summary {{ padding: 30px; background: #f8f9fa; border-bottom: 1px solid #dee2e6; }}
        .summary-grid {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 20px; margin-top: 20px; }}
        .summary-item {{ text-align: center; padding: 20px; background: white; border-radius: 8px; border-left: 4px solid {statusColor}; }}
        .summary-item h3 {{ margin: 0 0 5px 0; font-size: 32px; color: {statusColor}; }}
        .summary-item p {{ margin: 0; color: #666; font-size: 14px; }}
        .content {{ padding: 30px; }}
        .company-issue {{ margin-bottom: 30px; padding: 25px; border: 1px solid #dee2e6; border-radius: 8px; background: #fff; }}
        .company-header {{ display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; padding-bottom: 15px; border-bottom: 2px solid #f8f9fa; }}
        .company-name {{ font-size: 20px; font-weight: bold; color: #333; margin: 0; }}
        .company-stats {{ text-align: right; }}
        .badge {{ display: inline-block; padding: 4px 12px; border-radius: 20px; font-size: 12px; font-weight: bold; color: white; background: #dc3545; }}
        .review {{ margin-bottom: 20px; padding: 20px; background: #f8f9fa; border-left: 4px solid #dc3545; border-radius: 0 8px 8px 0; }}
        .review-header {{ display: flex; justify-content: between; align-items: center; margin-bottom: 10px; }}
        .review-author {{ font-weight: bold; color: #333; }}
        .review-rating {{ color: #ffc107; }}
        .review-date {{ color: #666; font-size: 14px; }}
        .review-text {{ margin: 10px 0; color: #555; font-style: italic; }}
        .footer {{ background: #333; color: white; padding: 20px 30px; text-align: center; }}
        .footer a {{ color: #667eea; text-decoration: none; }}
        .no-issues {{ text-align: center; padding: 40px; color: #28a745; }}
        .no-issues h2 {{ color: #28a745; margin-bottom: 15px; }}
        @media (max-width: 600px) {{
            .summary-grid {{ grid-template-columns: 1fr; }}
            .company-header {{ flex-direction: column; align-items: flex-start; }}
            .company-stats {{ margin-top: 10px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{statusIcon} Review Monitor Report</h1>
            <p>{monitorName}</p>
            <p>{report.FromDate:MMM dd, yyyy} - {report.ToDate:MMM dd, yyyy}</p>
        </div>
        
        <div class='summary'>
            <h2 style='margin-top: 0; color: #333;'>Executive Summary</h2>
            <div class='summary-grid'>
                <div class='summary-item'>
                    <h3>{report.TotalCompaniesChecked}</h3>
                    <p>Companies Checked</p>
                </div>
                <div class='summary-item'>
                    <h3 style='color: #dc3545;'>{report.CompaniesWithIssues}</h3>
                    <p>Need Attention</p>
                </div>
                <div class='summary-item'>
                    <h3 style='color: #ffc107;'>{report.TotalBadReviews}</h3>
                    <p>Bad Reviews</p>
                </div>
                <div class='summary-item'>
                    <h3 style='color: #28a745;'>{report.TotalCompaniesChecked - report.CompaniesWithIssues}</h3>
                    <p>Companies OK</p>
                </div>
            </div>
            <p style='margin: 20px 0 0 0; color: #666; text-align: center;'>
                <strong>Filter:</strong> {report.MaxRating} stars and below | 
                <strong>Period:</strong> {(report.ToDate - report.FromDate).Days} days | 
                <strong>Generated:</strong> {report.GeneratedAt:MMM dd, yyyy HH:mm}
            </p>
        </div>
        
        <div class='content'>";

            if (report.CompaniesWithIssues == 0)
            {
                html += @"
            <div class='no-issues'>
                <h2>🎉 Excellent News!</h2>
                <p>All companies are performing well with no bad reviews in the specified period.</p>
                <p>Keep up the great work!</p>
            </div>";
            }
            else
            {
                html += "<h2 style='color: #dc3545; margin-bottom: 25px;'>⚠️ Companies Requiring Attention</h2>";
                
                foreach (var company in report.CompanyReports.Take(10)) // Limit to top 10 for email
                {
                    html += $@"
            <div class='company-issue'>
                <div class='company-header'>
                    <h3 class='company-name'>{company.Company.Name}</h3>
                    <div class='company-stats'>
                        <span class='badge'>{company.TotalBadReviews} bad review{(company.TotalBadReviews != 1 ? "s" : "")}</span>
                        <div style='margin-top: 5px; color: #ffc107;'>
                            {string.Concat(Enumerable.Repeat("★", (int)Math.Round(company.AverageRating)))}
                            {string.Concat(Enumerable.Repeat("☆", 5 - (int)Math.Round(company.AverageRating)))}
                            ({company.AverageRating:F1})
                        </div>
                    </div>
                </div>";

                    foreach (var review in company.BadReviews.Take(3)) // Show top 3 worst reviews
                    {
                        var timeAgo = DateTime.UtcNow - review.Time;
                        var timeAgoText = timeAgo.Days > 0 ? $"{timeAgo.Days} days ago" : 
                                         timeAgo.Hours > 0 ? $"{timeAgo.Hours} hours ago" : 
                                         $"{timeAgo.Minutes} minutes ago";

                        html += $@"
                <div class='review'>
                    <div class='review-header'>
                        <span class='review-author'>{review.AuthorName}</span>
                        <span class='review-rating'>{string.Concat(Enumerable.Repeat("★", review.Rating))}{string.Concat(Enumerable.Repeat("☆", 5 - review.Rating))}</span>
                        <span class='review-date'>{timeAgoText}</span>
                    </div>
                    <div class='review-text'>{(string.IsNullOrEmpty(review.Text) ? "No review text provided" : review.Text)}</div>
                </div>";
                    }

                    html += "</div>";
                }

                if (report.CompanyReports.Count > 10)
                {
                    html += $"<p style='text-align: center; color: #666; margin-top: 20px;'><em>... and {report.CompanyReports.Count - 10} more companies with issues</em></p>";
                }
            }

            html += $@"
        </div>
        
        <div class='footer'>
            <p>This report was generated automatically by your Review Monitor System</p>
            <p>Generated at {report.GeneratedAt:MMM dd, yyyy HH:mm:ss}</p>
        </div>
    </div>
</body>
</html>";

            return html;
        }

        private string GenerateTestEmailHtml()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Test Email</title>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; background-color: #f8f9fa; }
        .container { max-width: 600px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); overflow: hidden; }
        .header { background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; }
        .content { padding: 30px; text-align: center; }
        .footer { background: #333; color: white; padding: 20px; text-align: center; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>✅ Test Email Successful</h1>
        </div>
        <div class='content'>
            <h2>Email Configuration Working!</h2>
            <p>Your Review Monitor System email configuration is working correctly.</p>
            <p>You will receive automated reports at this email address when scheduled reviews are executed.</p>
        </div>
        <div class='footer'>
            <p>Review Monitor System - Test Email</p>
            <p>Generated at " + DateTime.UtcNow.ToString("MMM dd, yyyy HH:mm:ss") + @"</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}