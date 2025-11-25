using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using google_reviews.Data;
using google_reviews.Services;
using google_reviews.Models;
using ClosedXML.Excel;

namespace google_reviews.Controllers
{
    public class DiagnosticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            ILogger<DiagnosticsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Health()
        {
            try
            {
                var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var dllInfo = new System.IO.FileInfo(appPath);

                var info = new
                {
                    Status = "OK",
                    Timestamp = DateTime.UtcNow,
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    ConnectionString = _context.Database.GetConnectionString()?.Substring(0, 50) + "...",
                    MachineName = Environment.MachineName,
                    DllPath = appPath,
                    DllLastModified = dllInfo.LastWriteTimeUtc,
                    ViewCompilationTest = "VIEW_UPDATE_TEST_MARKER_2025_09_15_14_45"
                };

                return Json(info);
            }
            catch (Exception ex)
            {
                return Json(new { Status = "Error", Message = ex.Message, Timestamp = DateTime.UtcNow });
            }
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var diagnostics = new
                {
                    DatabaseConnected = await CanConnectToDatabase(),
                    TablesExist = await TablesExist(),
                    RolesExist = await RolesExist(),
                    AdminUserExists = await AdminUserExists(),
                    UsersCount = _userManager.Users.Count(),
                    ErrorMessage = ""
                };

                ViewBag.Diagnostics = diagnostics;
                return View();
            }
            catch (Exception ex)
            {
                var diagnostics = new
                {
                    DatabaseConnected = false,
                    TablesExist = false,
                    RolesExist = false,
                    AdminUserExists = false,
                    UsersCount = 0,
                    ErrorMessage = ex.Message
                };

                ViewBag.Diagnostics = diagnostics;
                return View();
            }
        }

        private async Task<bool> CanConnectToDatabase()
        {
            try
            {
                await _context.Database.CanConnectAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TablesExist()
        {
            try
            {
                await _context.Users.CountAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> RolesExist()
        {
            return await _roleManager.RoleExistsAsync("Admin") && await _roleManager.RoleExistsAsync("User");
        }

        private async Task<bool> AdminUserExists()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            return adminUsers.Any();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAdminUser()
        {
            try
            {
                // Create admin user manually
                var adminUser = new IdentityUser
                {
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(adminUser, "AdminPassword123!");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    TempData["Success"] = "Admin user created successfully!";
                }
                else
                {
                    TempData["Error"] = $"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating admin user: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> TestManualReviewReport()
        {
            try
            {
                _logger.LogInformation("Starting manual review report test with 1-star reviews from last week");

                // Get date range: last 7 days
                var fromDate = DateTime.Now.AddDays(-7);
                var toDate = DateTime.Now;
                var maxRating = 1; // 1-star reviews only

                // Get ALL active companies
                var companies = await _context.Companies
                    .Where(c => c.IsActive && !string.IsNullOrEmpty(c.PlaceId))
                    .ToListAsync();

                var companyReports = new List<CompanyReviewReport>();

                foreach (var company in companies)
                {
                    // Get reviews within date range and rating filter
                    var reviews = await _context.Reviews
                        .Where(r => r.CompanyId == company.Id
                                   && r.Time >= fromDate
                                   && r.Time <= toDate
                                   && r.Rating <= maxRating)
                        .OrderByDescending(r => r.Time)
                        .ToListAsync();

                    if (reviews.Any())
                    {
                        companyReports.Add(new CompanyReviewReport
                        {
                            Company = company,
                            BadReviews = reviews,
                            AverageRating = reviews.Average(r => r.Rating),
                            TotalBadReviews = reviews.Count
                        });
                    }
                }

                // Create the report
                var report = new ReviewMonitorReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    MaxRating = maxRating,
                    GeneratedAt = DateTime.UtcNow,
                    TotalCompaniesChecked = companies.Count,
                    CompaniesWithIssues = companyReports.Count,
                    TotalBadReviews = companyReports.Sum(r => r.TotalBadReviews),
                    CompanyReports = companyReports
                };

                // Generate Excel attachment
                var excelService = HttpContext.RequestServices.GetRequiredService<IExcelService>();
                var excelData = excelService.GenerateReviewReportExcel(report);
                var fileName = $"manual_1star_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // Send email with Excel attachment
                var success = await _emailService.SendReviewReportEmailWithExcelAsync(
                    "emil.jones@searchminds.se",
                    report,
                    "Manual 1-Star Review Report - Last Week",
                    excelData,
                    fileName);

                if (success)
                {
                    _logger.LogInformation($"Manual 1-star review report sent successfully");
                    return Json(new {
                        Success = true,
                        Message = $"Manual 1-star review report sent! Check email for: {fileName}",
                        FileName = fileName,
                        FileSize = excelData.Length,
                        DateRange = $"{fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
                        MaxRating = maxRating,
                        CompaniesChecked = companies.Count,
                        CompaniesWithIssues = report.CompaniesWithIssues,
                        TotalBadReviews = report.TotalBadReviews
                    });
                }
                else
                {
                    return Json(new {
                        Success = false,
                        Message = "Failed to send manual review report"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestManualReviewReport");
                return Json(new {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckDatabaseData()
        {
            try
            {
                var companies = await _context.Companies
                    .Select(c => new { c.Id, c.Name, c.IsActive, c.PlaceId })
                    .Take(10)
                    .ToListAsync();

                var totalReviews = await _context.Reviews.CountAsync();

                var reviewSample = await _context.Reviews
                    .Select(r => new { r.CompanyId, r.AuthorName, r.Rating, r.Text })
                    .Take(5)
                    .ToListAsync();

                return Json(new {
                    TotalCompanies = companies.Count,
                    ActiveCompanies = companies.Count(c => c.IsActive),
                    CompaniesWithPlaceId = companies.Count(c => !string.IsNullOrEmpty(c.PlaceId)),
                    TotalReviews = totalReviews,
                    Companies = companies,
                    ReviewSample = reviewSample
                });
            }
            catch (Exception ex)
            {
                return Json(new {
                    Error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestRealReviewReportExcel()
        {
            try
            {
                _logger.LogInformation("Starting real review report Excel test with database data");

                // Get real companies from database
                var companies = await _context.Companies
                    .Where(c => c.IsActive && !string.IsNullOrEmpty(c.PlaceId))
                    .Take(3)
                    .ToListAsync();

                if (!companies.Any())
                {
                    return Json(new {
                        Success = false,
                        Message = "No active companies found in database"
                    });
                }

                // Get real reviews for these companies (all reviews, not just bad ones for testing)
                var companyReports = new List<CompanyReviewReport>();

                foreach (var company in companies)
                {
                    var reviews = await _context.Reviews
                        .Where(r => r.CompanyId == company.Id)
                        .OrderByDescending(r => r.Time)
                        .Take(5) // Get up to 5 reviews per company for testing
                        .ToListAsync();

                    if (reviews.Any())
                    {
                        // For testing purposes, include ALL reviews regardless of rating
                        companyReports.Add(new CompanyReviewReport
                        {
                            Company = company,
                            BadReviews = reviews, // Using all reviews for testing
                            AverageRating = reviews.Average(r => r.Rating),
                            TotalBadReviews = reviews.Count
                        });
                    }
                }

                if (!companyReports.Any())
                {
                    return Json(new {
                        Success = false,
                        Message = "No reviews found for active companies"
                    });
                }

                var realReport = new ReviewMonitorReport
                {
                    TotalBadReviews = companyReports.Sum(cr => cr.TotalBadReviews),
                    CompaniesWithIssues = companyReports.Count,
                    TotalCompaniesChecked = companies.Count,
                    FromDate = DateTime.Now.AddDays(-30), // Last 30 days
                    ToDate = DateTime.Now,
                    MaxRating = 3,
                    GeneratedAt = DateTime.Now,
                    CompanyReports = companyReports.OrderBy(r => r.AverageRating).ToList()
                };

                // Use the actual ExcelService to generate Swedish columns with real data
                var excelService = HttpContext.RequestServices.GetRequiredService<IExcelService>();
                var excelData = excelService.GenerateReviewReportExcel(realReport);
                var fileName = $"real_review_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                var success = await _emailService.SendReviewReportEmailWithExcelAsync(
                    "emil.jones@searchminds.se",
                    realReport,
                    "Real Data Excel Test",
                    excelData,
                    fileName);

                if (success)
                {
                    _logger.LogInformation($"Real review report Excel test sent successfully");
                    return Json(new {
                        Success = true,
                        Message = $"Real review report with Swedish Excel columns sent! Check email for: {fileName}",
                        FileName = fileName,
                        FileSize = excelData.Length,
                        Companies = companies.Count,
                        TotalReviews = realReport.TotalBadReviews,
                        CompaniesWithIssues = realReport.CompaniesWithIssues,
                        CompanyNames = companies.Select(c => c.Name).ToArray()
                    });
                }
                else
                {
                    return Json(new {
                        Success = false,
                        Message = "Failed to send real review report Excel test"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestRealReviewReportExcel");
                return Json(new {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestReviewReportExcel()
        {
            try
            {
                _logger.LogInformation("Starting review report Excel test with Swedish columns");

                var mockReport = new ReviewMonitorReport
                {
                    TotalBadReviews = 2,
                    CompaniesWithIssues = 1,
                    TotalCompaniesChecked = 1,
                    FromDate = DateTime.Now.AddDays(-7),
                    ToDate = DateTime.Now,
                    MaxRating = 2,
                    GeneratedAt = DateTime.Now,
                    CompanyReports = new List<CompanyReviewReport>
                    {
                        new CompanyReviewReport
                        {
                            Company = new Company
                            {
                                Name = "Test Restaurant AB",
                                EmailAddress = "test@restaurant.se",
                                PlaceId = "ChIJTest123",
                                GoogleMapsUrl = "https://www.google.com/maps/place/test"
                            },
                            BadReviews = new List<Review>
                            {
                                new Review
                                {
                                    AuthorName = "Missnöjd Kund",
                                    Rating = 1,
                                    Text = "Mycket dålig service och maten var kall.",
                                    Time = DateTime.Now.AddDays(-2)
                                },
                                new Review
                                {
                                    AuthorName = "Besviken Gäst",
                                    Rating = 2,
                                    Text = "Långsam service, maten kom efter 45 minuter.",
                                    Time = DateTime.Now.AddDays(-1)
                                }
                            }
                        }
                    }
                };

                // Use the actual ExcelService to generate Swedish columns
                var excelService = HttpContext.RequestServices.GetRequiredService<IExcelService>();
                var excelData = excelService.GenerateReviewReportExcel(mockReport);
                var fileName = $"review_report_test_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                var success = await _emailService.SendReviewReportEmailWithExcelAsync(
                    "emil.jones@searchminds.se",
                    mockReport,
                    "Swedish Excel Test",
                    excelData,
                    fileName);

                if (success)
                {
                    _logger.LogInformation($"Review report Excel test sent successfully with Swedish columns");
                    return Json(new {
                        Success = true,
                        Message = $"Review report with Swedish Excel columns sent successfully! Check email for: {fileName}",
                        FileName = fileName,
                        FileSize = excelData.Length,
                        Columns = "företagnamn, mailadress, rating, review, review link"
                    });
                }
                else
                {
                    return Json(new {
                        Success = false,
                        Message = "Failed to send review report Excel test"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestReviewReportExcel");
                return Json(new {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> TestEmailWithExcel()
        {
            try
            {
                _logger.LogInformation("Starting email with Excel attachment test");

                // Create test Excel file
                var excelData = CreateTestExcelFile();
                var fileName = $"test_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                // Create a mock report for the email
                var mockReport = new ReviewMonitorReport
                {
                    TotalBadReviews = 3,
                    CompaniesWithIssues = 1,
                    TotalCompaniesChecked = 2,
                    FromDate = DateTime.Now.AddDays(-7),
                    ToDate = DateTime.Now,
                    MaxRating = 2,
                    GeneratedAt = DateTime.Now,
                    CompanyReports = new List<CompanyReviewReport>
                    {
                        new CompanyReviewReport
                        {
                            Company = new Company
                            {
                                Name = "Test Company",
                                EmailAddress = "test@example.com"
                            },
                            BadReviews = new List<Review>
                            {
                                new Review
                                {
                                    AuthorName = "Test Author",
                                    Rating = 2,
                                    Text = "This is a test review",
                                    Time = DateTime.Now
                                }
                            }
                        }
                    }
                };

                // Send test email with Excel attachment
                // You should change this email to your test email address
                var testEmail = "emil.jones@searchminds.se"; // Change this to your email!

                var success = await _emailService.SendReviewReportEmailWithExcelAsync(
                    testEmail,
                    mockReport,
                    "Excel Attachment Test",
                    excelData,
                    fileName);

                if (success)
                {
                    _logger.LogInformation($"Test email with Excel attachment sent successfully to {testEmail}");
                    return Json(new {
                        Success = true,
                        Message = $"Test email with Excel attachment sent successfully to {testEmail}. Check your email!",
                        FileName = fileName,
                        FileSize = excelData.Length
                    });
                }
                else
                {
                    _logger.LogError("Failed to send test email with Excel attachment");
                    return Json(new {
                        Success = false,
                        Message = "Failed to send test email. Check email configuration and logs."
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestEmailWithExcel");
                return Json(new {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        private byte[] CreateTestExcelFile()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Test Data");

            // Create headers
            worksheet.Cell(1, 1).Value = "Test Column A";
            worksheet.Cell(1, 2).Value = "Test Column B";
            worksheet.Cell(1, 3).Value = "Test Column C";

            // Style headers
            var headerRange = worksheet.Range(1, 1, 1, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Add test data
            worksheet.Cell(2, 1).Value = "Test Data 1";
            worksheet.Cell(2, 2).Value = "Test Data 2";
            worksheet.Cell(2, 3).Value = "Test Data 3";

            worksheet.Cell(3, 1).Value = "Sample Row 1";
            worksheet.Cell(3, 2).Value = "Sample Row 2";
            worksheet.Cell(3, 3).Value = "Sample Row 3";

            worksheet.Cell(4, 1).Value = "Demo Entry A";
            worksheet.Cell(4, 2).Value = "Demo Entry B";
            worksheet.Cell(4, 3).Value = "Demo Entry C";

            // Auto-fit columns
            worksheet.ColumnsUsed().AdjustToContents();

            // Convert to byte array
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        [HttpGet]
        public async Task<IActionResult> FixPlaceIds()
        {
            try
            {
                _logger.LogInformation("Starting Place ID corruption fix");

                // Get all companies and check for corruption
                var companies = await _context.Companies.ToListAsync();
                var fixedCount = 0;
                var results = new List<object>();

                foreach (var company in companies)
                {
                    var originalPlaceId = company.PlaceId;
                    var wasCorrupted = false;

                    // Check if Place ID is corrupted (contains review text, company info, etc.)
                    if (!string.IsNullOrEmpty(company.PlaceId))
                    {
                        if (company.PlaceId.Contains("No reviews available") ||
                            company.PlaceId.Contains("Last updated") ||
                            company.PlaceId.Contains("Place ID:") ||
                            company.PlaceId.Contains("B&B") ||
                            company.PlaceId.Length > 100)
                        {
                            wasCorrupted = true;
                        }
                    }

                    // Apply specific fixes based on company name
                    switch (company.Name)
                    {
                        case "Krögers":
                            if (company.PlaceId != "ChIJZfgXlGTRV0YRUIVdUWnXc1M")
                            {
                                company.PlaceId = "ChIJZfgXlGTRV0YRUIVdUWnXc1M";
                                wasCorrupted = true;
                            }
                            break;
                        case "Familjeterapeuterna Syd AB":
                            if (company.PlaceId != "ChIJ4WAYH4y9U0YRXe06sXvxMGo")
                            {
                                company.PlaceId = "ChIJ4WAYH4y9U0YRXe06sXvxMGo";
                                wasCorrupted = true;
                            }
                            break;
                        case "Familjeterapeuterna Syd Södermalm Sthlm":
                            if (company.PlaceId != "0x465f9dc1dcaa0959")
                            {
                                company.PlaceId = "0x465f9dc1dcaa0959";
                                wasCorrupted = true;
                            }
                            break;
                        case "Searchminds":
                            if (company.PlaceId != "ChIJnytgooPRV0YRTCdMAyVMLt4")
                            {
                                company.PlaceId = "ChIJnytgooPRV0YRTCdMAyVMLt4";
                                wasCorrupted = true;
                            }
                            break;
                        case "familjeterapeuterna syd lund":
                            if (company.PlaceId != "ChIJKdrs-aKXU0YRPgCk6_o75X0")
                            {
                                company.PlaceId = "ChIJKdrs-aKXU0YRPgCk6_o75X0";
                                wasCorrupted = true;
                            }
                            break;
                        case "Familjeterapeuterna syd uppsala":
                            // This one should have NULL PlaceId according to backup
                            if (!string.IsNullOrEmpty(company.PlaceId))
                            {
                                company.PlaceId = null;
                                wasCorrupted = true;
                            }
                            break;
                    }

                    // If any other company has corrupted data, set PlaceId to null
                    if (wasCorrupted && !new[] { "Krögers", "Familjeterapeuterna Syd AB", "Familjeterapeuterna Syd Södermalm Sthlm", "Searchminds", "familjeterapeuterna syd lund", "Familjeterapeuterna syd uppsala" }.Contains(company.Name))
                    {
                        company.PlaceId = null;
                    }

                    if (wasCorrupted)
                    {
                        fixedCount++;
                        results.Add(new
                        {
                            CompanyName = company.Name,
                            OriginalPlaceId = originalPlaceId?.Length > 50 ? originalPlaceId.Substring(0, 50) + "..." : originalPlaceId,
                            NewPlaceId = company.PlaceId,
                            Status = "Fixed"
                        });
                    }
                    else
                    {
                        results.Add(new
                        {
                            CompanyName = company.Name,
                            OriginalPlaceId = originalPlaceId,
                            NewPlaceId = company.PlaceId,
                            Status = "OK"
                        });
                    }
                }

                // Save changes
                if (fixedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Fixed {fixedCount} corrupted Place IDs");
                }

                return Json(new
                {
                    Success = true,
                    Message = $"Place ID fix completed. Fixed {fixedCount} corrupted entries out of {companies.Count} total companies.",
                    FixedCount = fixedCount,
                    TotalCompanies = companies.Count,
                    Results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fixing Place IDs");
                return Json(new
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckPlaceIds()
        {
            try
            {
                var companies = await _context.Companies
                    .Select(c => new
                    {
                        c.Name,
                        c.PlaceId,
                        IsCorrupted = !string.IsNullOrEmpty(c.PlaceId) && (
                            c.PlaceId.Contains("No reviews available") ||
                            c.PlaceId.Contains("Last updated") ||
                            c.PlaceId.Contains("Place ID:") ||
                            c.PlaceId.Contains("B&B") ||
                            c.PlaceId.Length > 100
                        ),
                        PlaceIdLength = c.PlaceId != null ? c.PlaceId.Length : 0
                    })
                    .ToListAsync();

                return Json(new
                {
                    Success = true,
                    Companies = companies,
                    TotalCompanies = companies.Count,
                    CorruptedCount = companies.Count(c => c.IsCorrupted)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Place IDs");
                return Json(new
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                });
            }
        }
    }
}