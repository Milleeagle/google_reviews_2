 using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using google_reviews.Data;
using google_reviews.Models;
using google_reviews.Services;

namespace google_reviews.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IGooglePlacesService _googlePlacesService;
        private readonly ILogger<ReviewsController> _logger;

        public ReviewsController(
            ApplicationDbContext context,
            IGooglePlacesService googlePlacesService,
            ILogger<ReviewsController> logger)
        {
            _context = context;
            _googlePlacesService = googlePlacesService;
            _logger = logger;
        }

        // GET: Reviews
        public async Task<IActionResult> Index()
        {
            var companies = await _context.Companies
                .Include(c => c.Reviews)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(companies);
        }

        // GET: Reviews/Company/5
        public async Task<IActionResult> Company(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var company = await _context.Companies
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (company == null)
            {
                return NotFound();
            }

            // Order reviews by date (newest first)
            company.Reviews = company.Reviews.OrderByDescending(r => r.Time).ToList();

            return View(company);
        }

        // GET: Reviews/AddCompany
        [Authorize(Roles = "Admin")]
        public IActionResult AddCompany()
        {
            return View();
        }

        // POST: Reviews/AddCompany
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddCompany(Company company)
        {
            if (ModelState.IsValid)
            {
                // Check if company with same Place ID already exists
                if (!string.IsNullOrEmpty(company.PlaceId))
                {
                    var existingCompany = await _context.Companies
                        .FirstOrDefaultAsync(c => c.PlaceId == company.PlaceId);
                    
                    if (existingCompany != null)
                    {
                        ModelState.AddModelError("PlaceId", "A company with this Place ID already exists.");
                        return View(company);
                    }
                }

                company.Id = Guid.NewGuid().ToString();
                company.LastUpdated = DateTime.UtcNow;
                
                _context.Companies.Add(company);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Added new company: {company.Name}");
                TempData["Success"] = $"Company '{company.Name}' added successfully!";
                
                return RedirectToAction(nameof(Index));
            }
            
            return View(company);
        }

        // POST: Reviews/RefreshReviews/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RefreshReviews(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company == null)
            {
                return NotFound();
            }

            try
            {
                var reviewData = await _googlePlacesService.GetReviewsAsync(company);
                if (reviewData == null)
                {
                    TempData["Error"] = "Failed to fetch reviews from Google Places API.";
                    return RedirectToAction(nameof(Company), new { id });
                }

                // Remove existing reviews
                var existingReviews = await _context.Reviews.Where(r => r.CompanyId == id).ToListAsync();
                _context.Reviews.RemoveRange(existingReviews);

                // Add new reviews
                _context.Reviews.AddRange(reviewData.Reviews);
                
                // Update company last updated time
                company.LastUpdated = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Refreshed reviews for company {company.Name}: {reviewData.Reviews.Count} reviews");
                TempData["Success"] = $"Successfully refreshed {reviewData.Reviews.Count} reviews for {company.Name}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error refreshing reviews for company {company.Name}");
                TempData["Error"] = "An error occurred while refreshing reviews.";
            }

            return RedirectToAction(nameof(Company), new { id });
        }

        // GET: Reviews/FilteredReviews/5
        public async Task<IActionResult> FilteredReviews(string id, DateTime? fromDate = null, DateTime? toDate = null, int? minRating = null, int? maxRating = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company == null)
            {
                return NotFound();
            }

            try
            {
                var reviewData = await _googlePlacesService.GetFilteredReviewsAsync(company, fromDate, toDate, minRating, maxRating);
                if (reviewData == null)
                {
                    TempData["Error"] = "Failed to fetch filtered reviews from Google Places API.";
                    return RedirectToAction(nameof(Company), new { id });
                }

                ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
                ViewBag.MinRating = minRating;
                ViewBag.MaxRating = maxRating;
                ViewBag.IsFiltered = true;

                // Create a temporary company object with filtered data
                var filteredCompany = new Company
                {
                    Id = company.Id,
                    Name = company.Name,
                    PlaceId = company.PlaceId,
                    GoogleMapsUrl = company.GoogleMapsUrl,
                    IsActive = company.IsActive,
                    LastUpdated = company.LastUpdated,
                    Reviews = reviewData.Reviews
                };

                return View("Company", filteredCompany);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching filtered reviews for company {company.Name}");
                TempData["Error"] = "An error occurred while fetching filtered reviews.";
                return RedirectToAction(nameof(Company), new { id });
            }
        }

        // GET: Reviews/TestApi
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestApi()
        {
            var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = config["GooglePlaces:ApiKey"];
            
            var isConnected = await _googlePlacesService.TestApiConnectionAsync();
            ViewBag.ApiConnected = isConnected;
            ViewBag.ApiKey = !string.IsNullOrEmpty(apiKey);
            ViewBag.ApiKeyValue = string.IsNullOrEmpty(apiKey) ? "Not set" : $"{apiKey.Substring(0, 10)}...";
            
            return View();
        }

        // GET: Reviews/ReviewMonitor
        [Authorize(Roles = "Admin")]
        public IActionResult ReviewMonitor()
        {
            return View();
        }

        // POST: Reviews/GenerateReviewReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateReviewReport(DateTime fromDate, DateTime toDate, int maxRating = 3)
        {
            try
            {
                var companies = await _context.Companies
                    .Where(c => c.IsActive && !string.IsNullOrEmpty(c.PlaceId))
                    .ToListAsync();

                var reportData = new List<CompanyReviewReport>();
                
                foreach (var company in companies)
                {
                    try
                    {
                        var reviewData = await _googlePlacesService.GetFilteredReviewsAsync(
                            company, fromDate, toDate, null, maxRating);
                        
                        if (reviewData?.Reviews?.Any() == true)
                        {
                            var badReviews = reviewData.Reviews
                                .Where(r => r.Rating <= maxRating && r.Time >= fromDate && r.Time <= toDate)
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
                        _logger.LogError(ex, $"Error fetching reviews for company {company.Name}");
                    }
                }

                var report = new ReviewMonitorReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    MaxRating = maxRating,
                    CompanyReports = reportData.OrderBy(r => r.AverageRating).ToList(),
                    GeneratedAt = DateTime.UtcNow,
                    TotalCompaniesChecked = companies.Count,
                    CompaniesWithIssues = reportData.Count,
                    TotalBadReviews = reportData.Sum(r => r.TotalBadReviews)
                };

                return View("ReviewReport", report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating review report");
                TempData["Error"] = "An error occurred while generating the review report.";
                return RedirectToAction(nameof(ReviewMonitor));
            }
        }
    }
}