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

        public async Task<bool> SendBatchReviewNotificationAsync(string recipientEmail, List<CompanyReviewData> reviewData, List<string> results, int successCount, int failCount)
        {
            try
            {
                var emailConfig = _configuration.GetSection("Email");
                var smtpHost = emailConfig["SmtpHost"];
                var smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
                var smtpUsername = emailConfig["SmtpUsername"];
                var smtpPassword = emailConfig["SmtpPassword"];
                var fromEmail = emailConfig["FromEmail"] ?? smtpUsername;
                var fromName = emailConfig["FromName"] ?? "Google Reviews System";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogError("Email configuration is missing required settings");
                    return false;
                }

                // Find companies with negative reviews (rating <= 3)
                var companiesWithNegativeReviews = reviewData
                    .Where(rd => rd.Reviews.Any(r => r.Rating <= 3))
                    .ToList();

                var hasNegativeReviews = companiesWithNegativeReviews.Any();

                var subject = hasNegativeReviews
                    ? $"⚠️ Batch Review Alert: {companiesWithNegativeReviews.Count} companies have negative reviews"
                    : $"✅ Batch Review Complete: {successCount} companies processed successfully";

                var htmlBody = GenerateBatchReviewEmailHtml(reviewData, results, successCount, failCount, companiesWithNegativeReviews);

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
                _logger.LogInformation($"Batch review notification email sent successfully to {recipientEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send batch review notification email to {recipientEmail}");
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

                // Show all companies with issues (not limited to 10)
                foreach (var company in report.CompanyReports)
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

                    // For large reports, limit reviews per company to avoid email size limits
                    var reviewsToShow = report.CompanyReports.Count > 20 ? 1 : 3;
                    foreach (var review in company.BadReviews.Take(reviewsToShow))
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

                // Add pagination notice for very large reports to avoid email size limits
                if (report.CompanyReports.Count > 50)
                {
                    html += $"<p style='text-align: center; color: #666; margin-top: 20px;'><em>📧 Large report: Showing all {report.CompanyReports.Count} companies with issues</em></p>";
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

        private string GenerateBatchReviewEmailHtml(List<CompanyReviewData> reviewData, List<string> results, int successCount, int failCount, List<CompanyReviewData> companiesWithNegativeReviews)
        {
            var hasNegativeReviews = companiesWithNegativeReviews.Any();
            var statusColor = hasNegativeReviews ? "#dc3545" : "#28a745";
            var statusIcon = hasNegativeReviews ? "⚠️" : "✅";

            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Batch Review Report</title>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background-color: #f8f9fa; }}
        .container {{ max-width: 800px; margin: 0 auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, {statusColor}, {statusColor}dd); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; }}
        .summary {{ padding: 30px; background: #f8f9fa; border-bottom: 1px solid #dee2e6; }}
        .summary-stats {{ display: flex; justify-content: space-around; text-align: center; margin-top: 20px; }}
        .stat {{ padding: 15px; }}
        .stat-number {{ font-size: 32px; font-weight: bold; color: {statusColor}; }}
        .stat-label {{ color: #6c757d; font-size: 14px; margin-top: 5px; }}
        .content {{ padding: 30px; }}
        .alert {{ padding: 15px; border-radius: 5px; margin: 15px 0; }}
        .alert-warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; color: #856404; }}
        .company-list {{ margin: 20px 0; }}
        .company-item {{ padding: 15px; border: 1px solid #dee2e6; border-radius: 5px; margin-bottom: 10px; }}
        .company-name {{ font-weight: bold; color: #495057; }}
        .negative-review {{ background-color: #f8d7da; border-color: #f5c6cb; margin-top: 10px; padding: 10px; border-radius: 3px; }}
        .review-text {{ font-style: italic; margin-top: 5px; color: #721c24; }}
        .results-section {{ background: #f8f9fa; padding: 20px; border-radius: 5px; margin-top: 20px; }}
        .footer {{ background: #6c757d; color: white; text-align: center; padding: 20px; font-size: 14px; }}
        .rating-stars {{ color: #ffc107; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{statusIcon} Batch Review Processing Complete</h1>
        </div>

        <div class='summary'>
            <h2>Processing Summary</h2>
            <div class='summary-stats'>
                <div class='stat'>
                    <div class='stat-number' style='color: #28a745;'>{successCount}</div>
                    <div class='stat-label'>Successfully Processed</div>
                </div>
                <div class='stat'>
                    <div class='stat-number' style='color: #dc3545;'>{failCount}</div>
                    <div class='stat-label'>Failed</div>
                </div>
                <div class='stat'>
                    <div class='stat-number' style='color: #ffc107;'>{companiesWithNegativeReviews.Count}</div>
                    <div class='stat-label'>Companies with Negative Reviews</div>
                </div>
            </div>
        </div>

        <div class='content'>";

            if (hasNegativeReviews)
            {
                html += $@"
            <div class='alert alert-warning'>
                <strong>⚠️ Attention Required!</strong> {companiesWithNegativeReviews.Count} companies have received negative reviews (3 stars or below).
            </div>

            <h3>Companies with Negative Reviews</h3>
            <div class='company-list'>";

                foreach (var company in companiesWithNegativeReviews)
                {
                    var negativeReviews = company.Reviews.Where(r => r.Rating <= 3).OrderByDescending(r => r.Time).Take(3);
                    html += $@"
                <div class='company-item'>
                    <div class='company-name'>{company.CompanyName}</div>
                    <div style='margin-top: 5px; color: #6c757d;'>Average Rating: {company.AverageRating:F1} ⭐ ({company.TotalReviews} total reviews)</div>";

                    foreach (var review in negativeReviews)
                    {
                        var stars = string.Join("", Enumerable.Repeat("⭐", review.Rating)) + string.Join("", Enumerable.Repeat("☆", 5 - review.Rating));
                        html += $@"
                    <div class='negative-review'>
                        <strong>{review.AuthorName}</strong> - {stars} ({review.Rating}/5)
                        <div style='color: #6c757d; font-size: 12px;'>{review.Time:MMM dd, yyyy}</div>
                        <div class='review-text'>""{(review.Text?.Length > 200 ? review.Text.Substring(0, 200) + "..." : review.Text)}""</div>
                    </div>";
                    }

                    html += "</div>";
                }

                html += "</div>";
            }
            else
            {
                html += $@"
            <div class='alert' style='background-color: #d4edda; border-color: #c3e6cb; color: #155724;'>
                <strong>✅ Great news!</strong> No negative reviews were found in the processed companies.
            </div>";
            }

            html += $@"
            <div class='results-section'>
                <h3>Detailed Processing Results</h3>
                <ul>";

            foreach (var result in results.Take(20)) // Limit to first 20 results
            {
                html += $"<li>{result}</li>";
            }

            if (results.Count > 20)
            {
                html += $"<li><em>... and {results.Count - 20} more companies</em></li>";
            }

            html += $@"
                </ul>
            </div>
        </div>

        <div class='footer'>
            <p>Google Reviews Batch Processing System</p>
            <p>Generated on {DateTime.UtcNow:MMM dd, yyyy} at {DateTime.UtcNow:HH:mm} UTC</p>
        </div>
    </div>
</body>
</html>";

            return html;
        }

        public async Task<bool> SendReviewReportEmailWithExcelAsync(string recipientEmail, ReviewMonitorReport report, string monitorName, byte[] excelData, string excelFileName)
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

                // Add a note about the Excel attachment
                htmlBody = htmlBody.Replace("</div>\n    </div>\n\n    <div class='footer'>",
                    @"</div>
    </div>

    <div style='background: #e3f2fd; padding: 20px; margin: 20px 30px; border-radius: 8px; border-left: 4px solid #2196f3;'>
        <h3 style='margin: 0 0 10px 0; color: #1976d2;'>📊 Excel Report Attached</h3>
        <p style='margin: 0; color: #333;'>A detailed Excel spreadsheet with all review data (företagnamn, mailadress, rating, review, review link) is attached to this email for your analysis.</p>
    </div>

    <div class='footer'>");

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

                // Add Excel attachment
                if (excelData?.Length > 0)
                {
                    var memoryStream = new MemoryStream(excelData);
                    var attachment = new Attachment(memoryStream, excelFileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                    message.Attachments.Add(attachment);
                    _logger.LogInformation($"Added Excel attachment: {excelFileName} ({excelData.Length} bytes)");
                }
                else
                {
                    _logger.LogWarning("No Excel data to attach - excelData is null or empty");
                }

                await client.SendMailAsync(message);

                _logger.LogInformation($"Review report email with Excel attachment sent successfully to {recipientEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send review report email with Excel attachment to {recipientEmail}");
                return false;
            }
        }

        public async Task<bool> SendCustomerOutreachEmailAsync(CustomerEmailData customerData, string senderName, string senderPhone, string senderWebsite)
        {
            try
            {
                var emailConfig = _configuration.GetSection("Email");
                var smtpHost = emailConfig["SmtpHost"];
                var smtpPort = int.Parse(emailConfig["SmtpPort"] ?? "587");
                var smtpUsername = emailConfig["SmtpUsername"];
                var smtpPassword = emailConfig["SmtpPassword"];
                var fromEmail = emailConfig["FromEmail"] ?? smtpUsername;
                var fromName = emailConfig["FromName"] ?? "Deletify";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogError("Email configuration is missing required settings");
                    return false;
                }

                var subject = $"Gratis granskning av er Google-profil - {customerData.CompanyName}";
                var htmlBody = GenerateCustomerOutreachEmailHtml(customerData, senderName, senderPhone, senderWebsite);

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.EnableSsl = true;
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                var message = new MailMessage();
                message.From = new MailAddress(fromEmail, fromName);
                message.To.Add(customerData.Email);
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;
                message.BodyEncoding = Encoding.UTF8;

                await client.SendMailAsync(message);

                _logger.LogInformation($"Customer outreach email sent successfully to {customerData.Email} ({customerData.CompanyName})");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send customer outreach email to {customerData.Email} ({customerData.CompanyName})");
                return false;
            }
        }

        public async Task<BatchEmailResult> SendBatchCustomerOutreachEmailsAsync(List<CustomerEmailData> customers, string senderName, string senderPhone, string senderWebsite, bool isTestMode = false, string? testEmail = null)
        {
            var result = new BatchEmailResult
            {
                TotalEmails = customers.Count,
                IsTestMode = isTestMode,
                TestEmail = testEmail
            };

            foreach (var customer in customers)
            {
                try
                {
                    // In test mode, send all emails to the test email address
                    var emailToSend = isTestMode && !string.IsNullOrEmpty(testEmail)
                        ? new CustomerEmailData
                        {
                            CompanyName = customer.CompanyName,
                            Email = testEmail,
                            GoogleMapsUrl = customer.GoogleMapsUrl,
                            BadReviewCount = customer.BadReviewCount,
                            AverageRating = customer.AverageRating,
                            BadReviews = customer.BadReviews
                        }
                        : customer;

                    var sent = await SendCustomerOutreachEmailAsync(emailToSend, senderName, senderPhone, senderWebsite);

                    if (sent)
                    {
                        result.SuccessCount++;
                        result.SuccessfulEmails.Add($"{customer.CompanyName} ({customer.Email})");
                    }
                    else
                    {
                        result.FailCount++;
                        result.FailedEmails.Add($"{customer.CompanyName} ({customer.Email})");
                    }

                    // Small delay between emails to avoid rate limiting
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending email to {customer.CompanyName} ({customer.Email})");
                    result.FailCount++;
                    result.FailedEmails.Add($"{customer.CompanyName} ({customer.Email}) - Error: {ex.Message}");
                }
            }

            return result;
        }

        private string GenerateCustomerOutreachEmailHtml(CustomerEmailData customerData, string senderName, string senderPhone, string senderWebsite)
        {
            var sb = new StringBuilder();

            sb.Append(@"
<!DOCTYPE html>
<html lang='sv'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.8;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f9f9f9;
        }
        .email-container {
            background-color: #ffffff;
            border-radius: 12px;
            padding: 40px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }
        .header {
            text-align: center;
            margin-bottom: 30px;
            padding-bottom: 20px;
            border-bottom: 3px solid #e74c3c;
        }
        .company-name {
            color: #e74c3c;
            font-weight: bold;
            font-size: 1.1em;
        }
        .greeting {
            font-size: 16px;
            margin-bottom: 25px;
            color: #2c3e50;
        }
        .highlight-box {
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 20px;
            margin: 25px 0;
            border-radius: 6px;
        }
        .bullet-list {
            margin: 20px 0;
            padding-left: 0;
        }
        .bullet-list li {
            list-style: none;
            padding: 12px 0;
            padding-left: 30px;
            position: relative;
            font-size: 15px;
            line-height: 1.6;
        }
        .bullet-list li:before {
            content: '✓';
            position: absolute;
            left: 0;
            color: #27ae60;
            font-weight: bold;
            font-size: 18px;
        }
        .cta {
            text-align: center;
            margin: 35px 0;
            padding: 30px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            border-radius: 10px;
            color: white;
        }
        .cta-text {
            font-size: 18px;
            font-weight: 600;
            margin-bottom: 15px;
        }
        .cta-button {
            display: inline-block;
            padding: 15px 35px;
            background-color: #ffffff;
            color: #667eea;
            text-decoration: none;
            border-radius: 50px;
            font-weight: bold;
            font-size: 16px;
            margin-top: 15px;
            box-shadow: 0 4px 15px rgba(0,0,0,0.2);
            transition: all 0.3s ease;
        }
        .signature {
            margin-top: 40px;
            padding-top: 25px;
            border-top: 2px solid #ecf0f1;
            font-size: 15px;
        }
        .sender-name {
            font-weight: bold;
            color: #2c3e50;
            font-size: 17px;
            margin-bottom: 5px;
        }
        .company-tag {
            color: #e74c3c;
            font-weight: 600;
            font-size: 16px;
        }
        .contact-info {
            color: #7f8c8d;
            margin-top: 10px;
            font-size: 14px;
        }
        .contact-info a {
            color: #3498db;
            text-decoration: none;
        }
        .ps-section {
            margin-top: 25px;
            padding: 20px;
            background-color: #f8f9fa;
            border-radius: 8px;
            font-style: italic;
            color: #555;
        }
        .review-link {
            color: #e74c3c;
            text-decoration: none;
            font-weight: 600;
        }
        .review-link:hover {
            text-decoration: underline;
        }
        @media only screen and (max-width: 600px) {
            .email-container {
                padding: 25px;
            }
            body {
                padding: 10px;
            }
        }
    </style>
</head>
<body>
    <div class='email-container'>
        <div class='header'>
            <div style='font-size: 24px; font-weight: bold; color: #e74c3c; margin-bottom: 5px;'>Deletify</div>
            <div style='color: #7f8c8d; font-size: 14px;'>Experter på Google-recensioner</div>
        </div>

        <div class='greeting'>
            <p>Hej <span class='company-name'>").Append(customerData.CompanyName).Append(@"</span>,</p>
            <p>Jag heter ").Append(senderName).Append(@" och äger ett företag som heter <strong>Deletify</strong>. Vi hjälper företag att ta bort felaktiga eller missvisande Google-recensioner.</p>
        </div>

        <div class='highlight-box'>
            <p style='margin: 0; font-weight: 600;'>Vi har noterat ");

            if (customerData.BadReviewCount == 1)
            {
                sb.Append("en 1-stjärnig recension");
            }
            else
            {
                sb.Append($"{customerData.BadReviewCount} lågt betygsatta recensioner");
            }

            sb.Append(@" på er <a href='").Append(customerData.GoogleMapsUrl).Append(@"' class='review-link' target='_blank'>Google-profil</a> som påverkar er rating och försäljning negativt.</p>
        </div>

        <p style='font-weight: 600; font-size: 16px; color: #2c3e50; margin-top: 30px;'>Kortfattat:</p>
        <ul class='bullet-list'>
            <li>Jag gör en <strong>gratis snabbgranskning</strong></li>
            <li>Jag agerar för att få bort recensionen</li>
            <li>Ni betalar endast om jag lyckas — <strong>inga andra avgifter eller förbindelser</strong></li>
        </ul>

        <div class='cta'>
            <div class='cta-text'>Vill du ha en gratis granskning?</div>
            <p style='margin: 10px 0; font-size: 15px;'>Svara på detta mejl så skickar jag den direkt!</p>
        </div>

        <div class='signature'>
            <div class='sender-name'>").Append(senderName).Append(@"</div>
            <div class='company-tag'>Deletify</div>
            <div class='contact-info'>
                📞 ").Append(senderPhone).Append(@" · 🌐 <a href='").Append(senderWebsite).Append(@"' target='_blank'>").Append(senderWebsite).Append(@"</a>
            </div>
        </div>

        <div class='ps-section'>
            <strong>PS.</strong> Om du föredrar ett 2-minuters samtal så bokar jag gärna in det. Svara bara på detta mejl med din tillgänglighet!
        </div>
    </div>
</body>
</html>");

            return sb.ToString();
        }
    }
}