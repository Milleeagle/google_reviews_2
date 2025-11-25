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
        private readonly IEmailService _emailService;
        private readonly IExcelService _excelService;
        private readonly BatchProgressService _progressService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ReviewsController(
            ApplicationDbContext context,
            IGooglePlacesService googlePlacesService,
            ILogger<ReviewsController> logger,
            IEmailService emailService,
            IExcelService excelService,
            BatchProgressService progressService,
            IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _googlePlacesService = googlePlacesService;
            _logger = logger;
            _emailService = emailService;
            _excelService = excelService;
            _progressService = progressService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        // GET: Reviews
        public async Task<IActionResult> Index(string letter = "A")
        {
            // Ensure we have a valid letter
            if (string.IsNullOrEmpty(letter) || letter.Length != 1)
                letter = "A";

            letter = letter.ToUpper();

            // Get companies starting with the specified letter
            var companies = await _context.Companies
                .Include(c => c.Reviews)
                .Where(c => c.Name.StartsWith(letter))
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Get all first letters that have companies (for the navigation)
            var availableLetters = await _context.Companies
                .Where(c => !string.IsNullOrEmpty(c.Name))
                .Select(c => c.Name.Substring(0, 1).ToUpper())
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            // Get counts for each customer type for the current letter
            var currentCustomers = companies.Where(c => c.IsCurrentCustomer).ToList();
            var potentialCustomers = companies.Where(c => !c.IsCurrentCustomer).ToList();

            ViewBag.CurrentLetter = letter;
            ViewBag.AvailableLetters = availableLetters;
            ViewBag.CurrentCustomerCount = currentCustomers.Count;
            ViewBag.PotentialCustomerCount = potentialCustomers.Count;
            ViewBag.TotalCompanies = await _context.Companies.CountAsync();

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

        // GET: Reviews/ImportFromExcel
        [Authorize(Roles = "Admin")]
        public IActionResult ImportFromExcel()
        {
            return View();
        }

        // POST: Reviews/ImportFromExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportFromExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return Json(new { success = false, error = "Please select an Excel file to upload." });
            }

            // Validate file extension
            var extension = Path.GetExtension(excelFile.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                return Json(new { success = false, error = "Please upload a valid Excel file (.xlsx or .xls)" });
            }

            try
            {
                // Parse Excel file first
                List<Company> companies;
                using (var stream = excelFile.OpenReadStream())
                {
                    companies = _excelService.ImportCompaniesFromExcel(stream);
                }

                if (!companies.Any())
                {
                    return Json(new { success = false, error = "No companies found in the Excel file." });
                }

                // Create session for progress tracking
                var sessionId = _progressService.CreateSession();

                // Start background import
                _ = Task.Run(() => ImportCompaniesBackground(companies, sessionId));

                return Json(new { success = true, sessionId = sessionId, totalCompanies = companies.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Excel file");
                return Json(new { success = false, error = $"Error parsing file: {ex.Message}" });
            }
        }

        // Background import method
        private async Task ImportCompaniesBackground(List<Company> companies, string sessionId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReviewsController>>();

            try
            {
                int addedCount = 0;
                int skippedCount = 0;
                var startTime = DateTime.UtcNow;
                const int batchSize = 100; // Save in batches for better performance

                logger.LogInformation($"Starting Excel import for {companies.Count} companies (Session: {sessionId})");

                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = companies.Count,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Starting import...",
                    Status = "Initializing",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = false,
                    StartTime = startTime,
                    RequestsPerMinute = 0,
                    RateLimitInfo = "Processing companies..."
                });

                var companiesToAdd = new List<Company>();

                for (int i = 0; i < companies.Count; i++)
                {
                    var company = companies[i];
                    var currentTime = DateTime.UtcNow;
                    var elapsed = currentTime - startTime;
                    var avgTimePerItem = i > 0 ? elapsed.TotalSeconds / i : 0;
                    var remainingItems = companies.Count - i;
                    var estimatedRemaining = avgTimePerItem > 0 ? (int)(remainingItems * avgTimePerItem) : 0;

                    // Update progress
                    _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                    {
                        TotalItems = companies.Count,
                        ProcessedItems = i,
                        SuccessfulItems = addedCount,
                        FailedItems = skippedCount,
                        CurrentItem = $"Processing: {company.Name}",
                        Status = "Processing",
                        EstimatedRemainingSeconds = estimatedRemaining,
                        IsComplete = false,
                        StartTime = startTime,
                        RequestsPerMinute = i > 0 ? (i / elapsed.TotalMinutes) : 0,
                        RateLimitInfo = $"Batch {i / batchSize + 1} of {(companies.Count / batchSize) + 1}"
                    });

                    // Check if company already exists
                    var existingCompany = await context.Companies
                        .FirstOrDefaultAsync(c =>
                            (!string.IsNullOrEmpty(company.PlaceId) && c.PlaceId == company.PlaceId) ||
                            c.Name.ToLower() == company.Name.ToLower());

                    if (existingCompany != null)
                    {
                        skippedCount++;
                        logger.LogDebug($"Skipped duplicate: {company.Name}");
                        continue;
                    }

                    companiesToAdd.Add(company);
                    addedCount++;

                    // Save in batches
                    if (companiesToAdd.Count >= batchSize)
                    {
                        context.Companies.AddRange(companiesToAdd);
                        await context.SaveChangesAsync();
                        logger.LogInformation($"Saved batch of {companiesToAdd.Count} companies");
                        companiesToAdd.Clear();
                    }
                }

                // Save remaining companies
                if (companiesToAdd.Any())
                {
                    context.Companies.AddRange(companiesToAdd);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Saved final batch of {companiesToAdd.Count} companies");
                }

                // Final progress update
                var finalElapsed = DateTime.UtcNow - startTime;
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = companies.Count,
                    ProcessedItems = companies.Count,
                    SuccessfulItems = addedCount,
                    FailedItems = skippedCount,
                    CurrentItem = "Import completed!",
                    Status = "Complete",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = startTime,
                    RequestsPerMinute = companies.Count / finalElapsed.TotalMinutes,
                    RateLimitInfo = $"Completed in {finalElapsed.TotalMinutes:F1} minutes. Added: {addedCount}, Skipped: {skippedCount}"
                });

                logger.LogInformation($"Excel import completed: {addedCount} added, {skippedCount} skipped");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in Excel import session {sessionId}");
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = companies.Count,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Error occurred during import",
                    Status = "Error",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = DateTime.UtcNow,
                    RequestsPerMinute = 0,
                    RateLimitInfo = $"Error: {ex.Message}"
                });
            }
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

        // GET: Reviews/BatchReviews
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BatchReviews(string letter = "A")
        {
            // Ensure we have a valid letter
            if (string.IsNullOrEmpty(letter) || letter.Length != 1)
                letter = "A";

            letter = letter.ToUpper();

            var companies = await _context.Companies
                .Include(c => c.Reviews)
                .Where(c => !string.IsNullOrEmpty(c.PlaceId) && c.IsActive && c.Name.StartsWith(letter))
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Get all first letters that have companies (for the navigation)
            var availableLetters = await _context.Companies
                .Where(c => !string.IsNullOrEmpty(c.Name) && !string.IsNullOrEmpty(c.PlaceId) && c.IsActive)
                .Select(c => c.Name.Substring(0, 1).ToUpper())
                .Distinct()
                .OrderBy(l => l)
                .ToListAsync();

            // Group companies by review status
            var companiesWithoutReviews = companies.Where(c => !c.Reviews.Any()).ToList();
            var companiesWithOldReviews = companies
                .Where(c => c.Reviews.Any() && c.LastUpdated < DateTime.UtcNow.AddDays(-7))
                .ToList();
            var allEligibleCompanies = companies.ToList();

            ViewBag.CompaniesWithoutReviews = companiesWithoutReviews;
            ViewBag.CompaniesWithOldReviews = companiesWithOldReviews;
            ViewBag.AllEligibleCompanies = allEligibleCompanies;
            ViewBag.CurrentLetter = letter;
            ViewBag.AvailableLetters = availableLetters;
            ViewBag.TotalCompaniesInLetter = companies.Count;

            return View(companies);
        }

        // GET: Reviews/FetchNewCompanyReviews
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> FetchNewCompanyReviews()
        {
            // Find companies that have PlaceId but no reviews
            var newCompanies = await _context.Companies
                .Include(c => c.Reviews)
                .Where(c => !string.IsNullOrEmpty(c.PlaceId) && c.IsActive && !c.Reviews.Any())
                .OrderBy(c => c.Name)
                .ToListAsync();

            if (!newCompanies.Any())
            {
                TempData["Info"] = "No new companies found that need reviews fetched.";
                return RedirectToAction(nameof(Index));
            }

            // Start fetching reviews for all new companies
            int successCount = 0;
            int failCount = 0;
            var results = new List<string>();

            foreach (var company in newCompanies)
            {
                try
                {
                    _logger.LogInformation($"Fetching reviews for new company: {company.Name}");
                    var reviewData = await _googlePlacesService.GetReviewsAsync(company, enableThrottling: true);

                    if (reviewData != null && reviewData.Reviews.Any())
                    {
                        _context.Reviews.AddRange(reviewData.Reviews);
                        company.LastUpdated = DateTime.UtcNow;
                        successCount++;
                        results.Add($"✓ {company.Name}: {reviewData.Reviews.Count} reviews");
                        _logger.LogInformation($"✓ Fetched {reviewData.Reviews.Count} reviews for '{company.Name}'");
                    }
                    else
                    {
                        failCount++;
                        results.Add($"✗ {company.Name}: No reviews found");
                        _logger.LogWarning($"✗ No reviews found for '{company.Name}'");
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    results.Add($"✗ {company.Name}: Error - {ex.Message}");
                    _logger.LogError(ex, $"Error fetching reviews for company '{company.Name}'");
                }
            }

            if (successCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = $"Fetched reviews for {successCount} new companies. {failCount} failed.";
            if (results.Any())
            {
                TempData["Info"] = string.Join("<br/>", results);
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Reviews/ScrapeReviews/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ScrapeReviews(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company == null || string.IsNullOrEmpty(company.PlaceId))
            {
                TempData["Error"] = "Company not found or does not have a Place ID.";
                return RedirectToAction(nameof(Index));
            }

            IReviewScraper? scraper = null;
            try
            {
                _logger.LogInformation($"Scraping reviews for company: {company.Name}");

                // Create a new scraper instance for this request
                var scraperLogger = HttpContext.RequestServices.GetRequiredService<ILogger<GoogleReviewScraper>>();
                scraper = new GoogleReviewScraper(scraperLogger);
                var reviews = await scraper.ScrapeReviewsAsync(company.PlaceId);

                if (reviews != null && reviews.Any())
                {
                    // Remove existing reviews
                    var existingReviews = await _context.Reviews.Where(r => r.CompanyId == id).ToListAsync();
                    _context.Reviews.RemoveRange(existingReviews);

                    // Add CompanyId to scraped reviews
                    foreach (var review in reviews)
                    {
                        review.CompanyId = company.Id;
                    }

                    // Add new reviews
                    _context.Reviews.AddRange(reviews);

                    // Update company last updated time
                    company.LastUpdated = DateTime.UtcNow;

                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Scraped {reviews.Count} reviews for '{company.Name}'");
                    TempData["Success"] = $"Successfully scraped {reviews.Count} reviews for {company.Name} (FREE - No API cost!)";
                }
                else
                {
                    TempData["Warning"] = $"No reviews found for {company.Name} using scraper.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error scraping reviews for company {company.Name}");
                TempData["Error"] = "An error occurred while scraping reviews.";
            }
            finally
            {
                // Dispose of the scraper to clean up browser resources
                scraper?.Dispose();
            }

            return RedirectToAction(nameof(Company), new { id });
        }

        // POST: Reviews/StartBatchScrape - Parallel scraping
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> StartBatchScrape([FromForm] List<string>? selectedCompanyIds, [FromForm] bool scrapeAll = false)
        {
            _logger.LogInformation("StartBatchScrape endpoint hit");
            _logger.LogInformation($"User authenticated: {User.Identity?.IsAuthenticated}");
            _logger.LogInformation($"User in Admin role: {User.IsInRole("Admin")}");
            _logger.LogInformation($"Scrape All flag: {scrapeAll}");

            try
            {
                // If scrapeAll is true, get all company IDs from database
                if (scrapeAll)
                {
                    selectedCompanyIds = await _context.Companies
                        .Where(c => c.IsActive)
                        .OrderBy(c => c.Name)
                        .Select(c => c.Id.ToString())
                        .ToListAsync();
                    _logger.LogInformation($"Scrape All: Found {selectedCompanyIds.Count} active companies");
                }
                else
                {
                    // If FromForm binding didn't work, try manual reading
                    if (selectedCompanyIds == null || !selectedCompanyIds.Any())
                    {
                        selectedCompanyIds = Request.Form["selectedCompanyIds"].Where(id => !string.IsNullOrEmpty(id)).Select(id => id!).ToList();
                        _logger.LogInformation($"Manual form reading found {selectedCompanyIds?.Count ?? 0} IDs");
                    }
                    else
                    {
                        _logger.LogInformation($"FromForm binding found {selectedCompanyIds.Count} IDs");
                    }
                }

                if (selectedCompanyIds == null || !selectedCompanyIds.Any())
                {
                    _logger.LogWarning("No company IDs provided for batch scrape");
                    return Json(new { success = false, error = $"Please select at least one company to scrape reviews." });
                }

                _logger.LogInformation($"Starting batch scrape for {selectedCompanyIds.Count} companies");
                var sessionId = _progressService.CreateSession();

                // Create initial progress entry to avoid race condition
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = selectedCompanyIds.Count,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Initializing...",
                    Status = "Starting",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = false,
                    StartTime = DateTime.UtcNow,
                    RequestsPerMinute = 0,
                    RateLimitInfo = "FREE Web Scraping - No API costs!"
                });

                // Start the batch scrape process in the background
                _ = Task.Run(() => BatchScrapeReviewsBackground(selectedCompanyIds, sessionId));

                return Json(new { success = true, sessionId = sessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StartBatchScrape");
                return Json(new { success = false, error = $"Server error: {ex.Message}" });
            }
        }

        // GET: Reviews/GetAllCompanyIds - Returns all company IDs for "Scrape All" functionality
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCompanyIds()
        {
            try
            {
                var companyIds = await _context.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => c.Id.ToString())
                    .ToListAsync();

                return Json(new { success = true, companyIds = companyIds, count = companyIds.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all company IDs");
                return Json(new { success = false, error = $"Server error: {ex.Message}" });
            }
        }

        // GET: Reviews/GetBatchProgress - Get progress of batch scraping
        [HttpGet]
        [Route("Reviews/GetBatchProgress")]
        public IActionResult GetBatchProgress([FromQuery] string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("SessionId is required");
            }

            var progress = _progressService.GetProgress(sessionId);
            if (progress == null)
            {
                return NotFound(new { error = "Session not found", sessionId = sessionId });
            }

            return Json(progress);
        }

        // Background parallel batch scrape method
        private async Task BatchScrapeReviewsBackground(List<string> selectedCompanyIds, string sessionId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<ReviewsController>();

            try
            {
                // Skip companies where reviews were scraped in the last 96 hours (4 days)
                var cutoffDate = DateTime.UtcNow.AddHours(-96);

                var companies = await context.Companies
                    .Where(c => selectedCompanyIds.Contains(c.Id) &&
                               !string.IsNullOrEmpty(c.PlaceId) &&
                               (c.LastUpdated < cutoffDate || !c.Reviews.Any()))
                    .ToListAsync();

                var totalRequested = selectedCompanyIds.Count;
                var skippedCount = totalRequested - companies.Count;

                if (skippedCount > 0)
                {
                    logger.LogInformation($"Skipping {skippedCount} companies that had reviews scraped in the last 96 hours (4 days)");
                }

                int successCount = 0;
                int failCount = 0;
                var startTime = DateTime.UtcNow;

                logger.LogInformation($"========== BATCH SCRAPE STARTED ==========");
                logger.LogInformation($"Requested companies: {totalRequested}");
                logger.LogInformation($"Skipped (recently updated): {skippedCount}");
                logger.LogInformation($"Processing companies: {companies.Count}");
                logger.LogInformation($"Session: {sessionId}");
                logger.LogInformation($"==========================================");

                // Update initial progress
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = companies.Count,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Starting batch scrape...",
                    Status = "Starting",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = false,
                    StartTime = startTime,
                    RequestsPerMinute = 0,
                    RateLimitInfo = "FREE Web Scraping - Processing 10 companies in parallel!"
                });

                // Process companies in parallel batches of 10
                const int batchSize = 10;
                int processed = 0;

                for (int batchIndex = 0; batchIndex < companies.Count; batchIndex += batchSize)
                {
                    var batchCompanies = companies.Skip(batchIndex).Take(batchSize).ToList();
                    var batchStartTime = DateTime.UtcNow;

                    logger.LogInformation($"========== PROCESSING BATCH {batchIndex / batchSize + 1} ==========");
                    logger.LogInformation($"Batch contains {batchCompanies.Count} companies");
                    logger.LogInformation($"Progress: {processed}/{companies.Count} companies processed");

                    // Process this batch in parallel
                    var batchTasks = batchCompanies.Select(async company =>
                    {
                        IReviewScraper? scraper = null;
                        try
                        {
                            logger.LogInformation($"Scraping reviews for company: {company.Name} (PlaceId: {company.PlaceId})");

                            // Create a new scraper instance for this company
                            scraper = new GoogleReviewScraper(loggerFactory.CreateLogger<GoogleReviewScraper>());
                            var reviews = await scraper.ScrapeReviewsAsync(company.PlaceId!);

                            if (reviews != null && reviews.Any())
                            {
                                // Use a separate scope for database operations to avoid concurrency issues
                                using var dbScope = _serviceScopeFactory.CreateScope();
                                var dbContext = dbScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                                // Remove existing reviews
                                var existingReviews = await dbContext.Reviews
                                    .Where(r => r.CompanyId == company.Id)
                                    .ToListAsync();
                                dbContext.Reviews.RemoveRange(existingReviews);

                                // Add CompanyId to scraped reviews
                                foreach (var review in reviews)
                                {
                                    review.CompanyId = company.Id;
                                }

                                // Add new reviews
                                dbContext.Reviews.AddRange(reviews);

                                // Update company last updated time
                                var companyToUpdate = await dbContext.Companies.FindAsync(company.Id);
                                if (companyToUpdate != null)
                                {
                                    companyToUpdate.LastUpdated = DateTime.UtcNow;
                                }

                                await dbContext.SaveChangesAsync();

                                Interlocked.Increment(ref successCount);
                                logger.LogInformation($"✓ Scraped {reviews.Count} reviews for '{company.Name}'");
                            }
                            else
                            {
                                Interlocked.Increment(ref failCount);
                                logger.LogWarning($"✗ No reviews found for '{company.Name}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref failCount);
                            logger.LogError(ex, $"Error scraping reviews for company '{company.Name}': {ex.Message}");
                        }
                        finally
                        {
                            // Dispose of the scraper to clean up browser resources
                            scraper?.Dispose();

                            // Force garbage collection after every 100 companies
                            if (successCount % 100 == 0)
                            {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();
                            }
                        }
                    }).ToList();

                    // Wait for all tasks in this batch to complete
                    await Task.WhenAll(batchTasks);

                    processed += batchCompanies.Count;

                    var currentTime = DateTime.UtcNow;
                    var elapsed = currentTime - startTime;
                    var avgTimePerItem = processed > 0 ? elapsed.TotalSeconds / processed : 0;
                    var remainingItems = companies.Count - processed;
                    var estimatedRemaining = avgTimePerItem > 0 ? (int)(remainingItems * avgTimePerItem) : 0;

                    // Update progress after each batch
                    _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                    {
                        TotalItems = companies.Count,
                        ProcessedItems = processed,
                        SuccessfulItems = successCount,
                        FailedItems = failCount,
                        CurrentItem = $"Completed batch {batchIndex / batchSize + 1}",
                        Status = "Processing",
                        EstimatedRemainingSeconds = estimatedRemaining,
                        IsComplete = false,
                        StartTime = startTime,
                        RequestsPerMinute = processed > 0 ? (processed / elapsed.TotalMinutes) : 0,
                        RateLimitInfo = $"FREE Web Scraping - Processing {processed}/{companies.Count} (5 at a time)"
                    });

                    var batchElapsed = DateTime.UtcNow - batchStartTime;
                    logger.LogInformation($"========== BATCH {batchIndex / batchSize + 1} COMPLETE ==========");
                    logger.LogInformation($"Batch time: {batchElapsed.TotalSeconds:F1} seconds");
                    logger.LogInformation($"Overall progress: {processed}/{companies.Count} companies");
                    logger.LogInformation($"Success: {successCount}, Failed: {failCount}");

                    // Small delay between batches to let resources settle
                    if (processed < companies.Count)
                    {
                        logger.LogInformation($"Waiting 3 seconds before starting next batch...");
                        await Task.Delay(3000); // 3 second delay between batches
                    }
                }

                // Final progress update
                var finalElapsed = DateTime.UtcNow - startTime;
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = companies.Count,
                    ProcessedItems = companies.Count,
                    SuccessfulItems = successCount,
                    FailedItems = failCount,
                    CurrentItem = "Batch scrape completed!",
                    Status = "Complete",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = startTime,
                    RequestsPerMinute = companies.Count / finalElapsed.TotalMinutes,
                    RateLimitInfo = $"Completed in {finalElapsed.TotalMinutes:F1} minutes. Success: {successCount}, Failed: {failCount}"
                });

                logger.LogInformation($"========== BATCH SCRAPE COMPLETE ==========");
                logger.LogInformation($"Requested companies: {totalRequested}");
                logger.LogInformation($"Skipped (recently updated): {skippedCount}");
                logger.LogInformation($"Processed: {companies.Count}");
                logger.LogInformation($"Successful: {successCount}");
                logger.LogInformation($"Failed: {failCount}");
                logger.LogInformation($"Total time: {finalElapsed.TotalMinutes:F1} minutes");
                logger.LogInformation($"Average rate: {(companies.Count / finalElapsed.TotalMinutes):F1} companies/min");
                logger.LogInformation($"==========================================");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"========== BATCH SCRAPE FAILED ==========");
                logger.LogError(ex, $"Session {sessionId} encountered an error");

                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = 0,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Error occurred during batch scrape",
                    Status = "Error",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = DateTime.UtcNow,
                    RequestsPerMinute = 0,
                    RateLimitInfo = $"Error: {ex.Message}"
                });
            }
        }

        // POST: Reviews/StartBatchRefresh - Returns session ID for polling
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> StartBatchRefresh([FromForm] List<string> selectedCompanyIds, [FromForm] string letter = "A", [FromForm] bool enableThrottling = true)
        {
            if (selectedCompanyIds == null || !selectedCompanyIds.Any())
            {
                return Json(new { success = false, error = "Please select at least one company to refresh reviews." });
            }

            var sessionId = _progressService.CreateSession();

            // Start the batch process in the background
            _ = Task.Run(() => BatchRefreshReviewsBackground(selectedCompanyIds, letter, enableThrottling, sessionId));

            return Json(new { success = true, sessionId = sessionId });
        }

        // Background batch refresh method
        private async Task BatchRefreshReviewsBackground(List<string> selectedCompanyIds, string letter, bool enableThrottling, string sessionId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var googlePlacesService = scope.ServiceProvider.GetRequiredService<IGooglePlacesService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReviewsController>>();

            try
            {
                var companies = await context.Companies
                    .Where(c => selectedCompanyIds.Contains(c.Id))
                    .ToListAsync();

                int successCount = 0;
                int failCount = 0;
                var results = new List<string>();
                var startTime = DateTime.UtcNow;

                logger.LogInformation($"Starting batch review refresh for {companies.Count} companies (Session: {sessionId})");

                // Send initial progress update
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = companies.Count,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Starting batch process...",
                    Status = "Initializing",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = false,
                    StartTime = startTime,
                    RequestsPerMinute = 0,
                    RateLimitInfo = enableThrottling ? "Rate limiting enabled (5000/min)" : "Rate limiting disabled"
                });

                for (int i = 0; i < companies.Count; i++)
                {
                    var company = companies[i];
                    var currentTime = DateTime.UtcNow;
                    var elapsed = currentTime - startTime;
                    var avgTimePerItem = i > 0 ? elapsed.TotalSeconds / i : 0;
                    var remainingItems = companies.Count - i;
                    var estimatedRemaining = avgTimePerItem > 0 ? (int)(remainingItems * avgTimePerItem) : 0;

                    // Send progress update before processing each company
                    _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                    {
                        TotalItems = companies.Count,
                        ProcessedItems = i,
                        SuccessfulItems = successCount,
                        FailedItems = failCount,
                        CurrentItem = $"Processing: {company.Name}",
                        Status = "Processing",
                        EstimatedRemainingSeconds = estimatedRemaining,
                        IsComplete = false,
                        StartTime = startTime,
                        RequestsPerMinute = i > 0 ? (i / elapsed.TotalMinutes) : 0,
                        RecentResults = results.TakeLast(5).ToList(),
                        RateLimitInfo = enableThrottling ? "Rate limiting enabled (5000/min)" : "Rate limiting disabled"
                    });

                    try
                    {
                        logger.LogDebug($"Processing reviews for company '{company.Name}'");

                        var reviewData = await googlePlacesService.GetReviewsAsync(company, enableThrottling);
                        if (reviewData != null)
                        {
                            // Remove existing reviews for this company
                            var existingReviews = await context.Reviews
                                .Where(r => r.CompanyId == company.Id)
                                .ToListAsync();
                            context.Reviews.RemoveRange(existingReviews);

                            // Add new reviews
                            context.Reviews.AddRange(reviewData.Reviews);

                            // Update company last updated time
                            company.LastUpdated = DateTime.UtcNow;

                            successCount++;
                            results.Add($"✓ {company.Name}: {reviewData.Reviews.Count} reviews");
                            logger.LogInformation($"✓ Fetched {reviewData.Reviews.Count} reviews for '{company.Name}'");
                        }
                        else
                        {
                            failCount++;
                            results.Add($"✗ {company.Name}: Failed to fetch reviews");
                            logger.LogWarning($"✗ Failed to fetch reviews for '{company.Name}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        results.Add($"✗ {company.Name}: Error - {ex.Message}");
                        logger.LogError(ex, $"Error fetching reviews for company '{company.Name}'");
                    }
                }

                if (successCount > 0)
                {
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Batch review refresh completed: {successCount} successful, {failCount} failed");
                }

                // Send final progress update
                var finalElapsed = DateTime.UtcNow - startTime;
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = companies.Count,
                    ProcessedItems = companies.Count,
                    SuccessfulItems = successCount,
                    FailedItems = failCount,
                    CurrentItem = "Batch processing completed!",
                    Status = "Complete",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = startTime,
                    RequestsPerMinute = companies.Count / finalElapsed.TotalMinutes,
                    RecentResults = results.TakeLast(10).ToList(),
                    RateLimitInfo = $"Completed in {finalElapsed.TotalMinutes:F1} minutes"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in batch processing session {sessionId}");
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = 0,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Error occurred during processing",
                    Status = "Error",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = DateTime.UtcNow,
                    RequestsPerMinute = 0,
                    RecentResults = new List<string> { $"Error: {ex.Message}" },
                    RateLimitInfo = "Processing failed"
                });
            }
        }

        // POST: Reviews/StartReviewReport - Returns session ID for polling
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> StartReviewReport([FromForm] DateTime fromDate, [FromForm] DateTime toDate, [FromForm] int maxRating = 3, [FromForm] string? notificationEmail = null, [FromForm] string? sendEmailAlert = null, [FromForm] string? selectedCompanyIds = null)
        {
            try
            {
                var sessionId = _progressService.CreateSession();

                // Create initial progress entry immediately so polling doesn't get 404
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = 0,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Initializing...",
                    Status = "Starting",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = false,
                    StartTime = DateTime.UtcNow,
                    RequestsPerMinute = 0,
                    RateLimitInfo = "Starting review report generation..."
                });

                // Start the report generation in the background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await GenerateReviewReportBackground(fromDate, toDate, maxRating, notificationEmail, sendEmailAlert, selectedCompanyIds, sessionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error in background task for session {sessionId}");
                        _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                        {
                            TotalItems = 0,
                            ProcessedItems = 0,
                            SuccessfulItems = 0,
                            FailedItems = 0,
                            CurrentItem = $"Error: {ex.Message}",
                            Status = "Error",
                            EstimatedRemainingSeconds = 0,
                            IsComplete = true,
                            StartTime = DateTime.UtcNow,
                            RequestsPerMinute = 0,
                            RateLimitInfo = "Task failed"
                        });
                    }
                });

                return Json(new { success = true, sessionId = sessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting review report generation");
                return Json(new { success = false, error = "Failed to start report generation" });
            }
        }

        // Background review report generation
        private async Task GenerateReviewReportBackground(DateTime fromDate, DateTime toDate, int maxRating, string? notificationEmail, string? sendEmailAlert, string? selectedCompanyIds, string sessionId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var googlePlacesService = scope.ServiceProvider.GetRequiredService<IGooglePlacesService>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var excelService = scope.ServiceProvider.GetRequiredService<IExcelService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReviewsController>>();

            try
            {
                var startTime = DateTime.UtcNow;
                bool shouldSendEmail = !string.IsNullOrEmpty(sendEmailAlert) && sendEmailAlert.ToLower() == "true";

                logger.LogInformation($"Starting review report generation (Session: {sessionId}). SendEmail: {shouldSendEmail}, Email: {notificationEmail}");

                // Get companies to process (without loading reviews to avoid memory issues)
                IQueryable<Company> companiesQuery = context.Companies;

                if (!string.IsNullOrEmpty(selectedCompanyIds))
                {
                    var companyIdList = selectedCompanyIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    companiesQuery = companiesQuery.Where(c => companyIdList.Contains(c.Id));
                }

                var companies = await companiesQuery.ToListAsync();

                // Send initial progress update
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = companies.Count,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Starting review report generation...",
                    Status = "Initializing",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = false,
                    StartTime = startTime,
                    RequestsPerMinute = 0,
                    RateLimitInfo = "Analyzing companies for review monitoring..."
                });

                var reportData = new List<string>();
                var companiesWithIssues = new List<Company>();
                int processedCount = 0;
                int companiesWithReviews = 0;
                int companiesWithBadReviews = 0;
                int totalBadReviewsFound = 0;

                for (int i = 0; i < companies.Count; i++)
                {
                    var company = companies[i];
                    var currentTime = DateTime.UtcNow;
                    var elapsed = currentTime - startTime;
                    var avgTimePerItem = i > 0 ? elapsed.TotalSeconds / i : 0;
                    var remainingItems = companies.Count - i;
                    var estimatedRemaining = avgTimePerItem > 0 ? (int)(remainingItems * avgTimePerItem) : 0;

                    // Send progress update
                    _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                    {
                        TotalItems = companies.Count,
                        ProcessedItems = i,
                        SuccessfulItems = companiesWithReviews,
                        FailedItems = i - companiesWithReviews,
                        CurrentItem = $"Analyzing: {company.Name}",
                        Status = "Processing",
                        EstimatedRemainingSeconds = estimatedRemaining,
                        IsComplete = false,
                        StartTime = startTime,
                        RequestsPerMinute = i > 0 ? (i / elapsed.TotalMinutes) : 0,
                        RecentResults = reportData.TakeLast(5).ToList(),
                        RateLimitInfo = $"Found {companiesWithBadReviews} companies with issues"
                    });

                    processedCount++;

                    // Load reviews separately to avoid memory issues
                    var relevantReviews = await context.Reviews
                        .Where(r => r.CompanyId == company.Id && r.Time >= fromDate && r.Time <= toDate)
                        .ToListAsync();

                    if (relevantReviews.Any())
                    {
                        companiesWithReviews++;

                        // Check for bad reviews
                        var badReviews = relevantReviews.Where(r => r.Rating <= maxRating).ToList();

                        if (badReviews.Any())
                        {
                            companiesWithBadReviews++;
                            companiesWithIssues.Add(company);
                            totalBadReviewsFound += badReviews.Count;

                            var resultMessage = $"✓ {company.Name}: {badReviews.Count} bad reviews (avg: {relevantReviews.Average(r => r.Rating):F1}★)";
                            reportData.Add(resultMessage);
                        }
                    }
                }

                // Load reviews for companies with issues to create detailed report
                var companyReportsWithReviews = new List<CompanyReviewReport>();

                foreach (var company in companiesWithIssues)
                {
                    var badReviews = await context.Reviews
                        .Where(r => r.CompanyId == company.Id && r.Time >= fromDate && r.Time <= toDate && r.Rating <= maxRating)
                        .OrderBy(r => r.Rating)
                        .ToListAsync();

                    companyReportsWithReviews.Add(new CompanyReviewReport
                    {
                        Company = company,
                        BadReviews = badReviews
                    });
                }

                // Create proper ReviewMonitorReport object
                var report = new ReviewMonitorReport
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    MaxRating = maxRating,
                    GeneratedAt = DateTime.UtcNow,
                    TotalCompaniesChecked = companies.Count,
                    CompaniesWithIssues = companiesWithBadReviews,
                    TotalBadReviews = totalBadReviewsFound,
                    CompanyReports = companyReportsWithReviews
                };

                // Store report for display
                _progressService.StoreReport(sessionId, report);

                // Send email if requested
                var finalElapsed = DateTime.UtcNow - startTime;
                if (shouldSendEmail && !string.IsNullOrEmpty(notificationEmail))
                {
                    _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                    {
                        TotalItems = companies.Count,
                        ProcessedItems = companies.Count,
                        SuccessfulItems = companiesWithReviews,
                        FailedItems = companies.Count - companiesWithReviews,
                        CurrentItem = "Sending email report...",
                        Status = "Sending Email",
                        EstimatedRemainingSeconds = 0,
                        IsComplete = false,
                        StartTime = startTime,
                        RequestsPerMinute = companies.Count / finalElapsed.TotalMinutes,
                        RecentResults = new List<string> { $"Found {companiesWithBadReviews} companies with review issues" },
                        RateLimitInfo = $"Sending email to {notificationEmail}..."
                    });

                    // Generate Excel attachment for manual report
                    var excelData = excelService.GenerateReviewReportExcel(report);
                    var fileName = $"manual_review_report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    await emailService.SendReviewReportEmailWithExcelAsync(notificationEmail, report, "Manual Review Report", excelData, fileName);
                }

                // Final complete status with redirect
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = companies.Count,
                    ProcessedItems = companies.Count,
                    SuccessfulItems = companiesWithReviews,
                    FailedItems = companies.Count - companiesWithReviews,
                    CurrentItem = shouldSendEmail && !string.IsNullOrEmpty(notificationEmail) ? "Report ready and email sent!" : "Report ready!",
                    Status = "Complete",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = startTime,
                    RequestsPerMinute = companies.Count / finalElapsed.TotalMinutes,
                    RecentResults = shouldSendEmail && !string.IsNullOrEmpty(notificationEmail) ?
                        new List<string> { $"Report emailed to {notificationEmail}", $"Found {companiesWithBadReviews} companies with issues" } :
                        new List<string> { $"Found {companiesWithBadReviews} companies with issues" },
                    RateLimitInfo = $"Completed in {finalElapsed.TotalMinutes:F1} minutes",
                    RedirectUrl = $"/Reviews/ViewReportResult?sessionId={sessionId}"
                });

                logger.LogInformation($"Review report generation completed (Session: {sessionId}). Processed {processedCount} companies, found {companiesWithBadReviews} with issues");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in review report generation session {sessionId}");
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = 0,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Error occurred during report generation",
                    Status = "Error",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = DateTime.UtcNow,
                    RequestsPerMinute = 0,
                    RecentResults = new List<string> { $"Error: {ex.Message}" },
                    RateLimitInfo = "Report generation failed"
                });
            }
        }

        // POST: Reviews/StartBatchAllReviews - Process ALL companies (no letter filtering)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> StartBatchAllReviews([FromForm] bool enableThrottling = true)
        {
            try
            {
                var sessionId = _progressService.CreateSession();

                // Start the batch process for ALL companies in the background
                _ = Task.Run(() => BatchAllReviewsBackground(enableThrottling, sessionId));

                return Json(new { success = true, sessionId = sessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting batch all reviews");
                return Json(new { success = false, error = "Failed to start batch all reviews" });
            }
        }

        // Background batch refresh method for ALL companies
        private async Task BatchAllReviewsBackground(bool enableThrottling, string sessionId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var googlePlacesService = scope.ServiceProvider.GetRequiredService<IGooglePlacesService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReviewsController>>();

            try
            {
                // Get ALL companies with Place IDs (no letter filtering)
                var companies = await context.Companies
                    .Where(c => !string.IsNullOrEmpty(c.PlaceId) && c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                int successCount = 0;
                int failCount = 0;
                var results = new List<string>();
                var startTime = DateTime.UtcNow;

                logger.LogInformation($"Starting batch ALL reviews refresh for {companies.Count} companies (Session: {sessionId})");

                // Send initial progress update
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = companies.Count,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Starting batch ALL reviews process...",
                    Status = "Initializing",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = false,
                    StartTime = startTime,
                    RequestsPerMinute = 0,
                    RateLimitInfo = enableThrottling ? "Rate limiting enabled (5000/min) - Processing ALL companies" : "Rate limiting disabled - Processing ALL companies"
                });

                for (int i = 0; i < companies.Count; i++)
                {
                    var company = companies[i];
                    var currentTime = DateTime.UtcNow;
                    var elapsed = currentTime - startTime;
                    var avgTimePerItem = i > 0 ? elapsed.TotalSeconds / i : 0;
                    var remainingItems = companies.Count - i;
                    var estimatedRemaining = avgTimePerItem > 0 ? (int)(remainingItems * avgTimePerItem) : 0;

                    // Send progress update before processing each company
                    _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                    {
                        TotalItems = companies.Count,
                        ProcessedItems = i,
                        SuccessfulItems = successCount,
                        FailedItems = failCount,
                        CurrentItem = $"Processing: {company.Name} (All companies {i + 1}/{companies.Count})",
                        Status = "Processing",
                        EstimatedRemainingSeconds = estimatedRemaining,
                        IsComplete = false,
                        StartTime = startTime,
                        RequestsPerMinute = i > 0 ? (i / elapsed.TotalMinutes) : 0,
                        RecentResults = results.TakeLast(5).ToList(),
                        RateLimitInfo = enableThrottling ? $"Rate limiting enabled (5000/min) - Processing ALL companies" : "Rate limiting disabled - Processing ALL companies"
                    });

                    try
                    {
                        logger.LogDebug($"Processing reviews for company '{company.Name}' (ALL companies batch)");

                        var reviewData = await googlePlacesService.GetReviewsAsync(company, enableThrottling);
                        if (reviewData != null)
                        {
                            // Remove existing reviews for this company
                            var existingReviews = await context.Reviews
                                .Where(r => r.CompanyId == company.Id)
                                .ToListAsync();
                            context.Reviews.RemoveRange(existingReviews);

                            // Add new reviews
                            context.Reviews.AddRange(reviewData.Reviews);

                            // Update company last updated time
                            company.LastUpdated = DateTime.UtcNow;

                            successCount++;
                            results.Add($"✓ {company.Name}: {reviewData.Reviews.Count} reviews");
                            logger.LogInformation($"✓ Fetched {reviewData.Reviews.Count} reviews for '{company.Name}' (ALL companies batch)");
                        }
                        else
                        {
                            failCount++;
                            results.Add($"✗ {company.Name}: Failed to fetch reviews");
                            logger.LogWarning($"✗ Failed to fetch reviews for '{company.Name}' (ALL companies batch)");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        results.Add($"✗ {company.Name}: Error - {ex.Message}");
                        logger.LogError(ex, $"Error fetching reviews for company '{company.Name}' (ALL companies batch)");
                    }
                }

                if (successCount > 0)
                {
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Batch ALL reviews refresh completed: {successCount} successful, {failCount} failed");
                }

                // Send final progress update
                var finalElapsed = DateTime.UtcNow - startTime;
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = companies.Count,
                    ProcessedItems = companies.Count,
                    SuccessfulItems = successCount,
                    FailedItems = failCount,
                    CurrentItem = "Batch ALL reviews processing completed!",
                    Status = "Complete",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = startTime,
                    RequestsPerMinute = companies.Count / finalElapsed.TotalMinutes,
                    RecentResults = results.TakeLast(10).ToList(),
                    RateLimitInfo = $"Completed ALL companies in {finalElapsed.TotalMinutes:F1} minutes"
                });

                logger.LogInformation($"Batch ALL reviews refresh completed (Session: {sessionId}). Processed {companies.Count} companies total");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in batch ALL reviews session {sessionId}");
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = 0,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Error occurred during ALL companies processing",
                    Status = "Error",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = DateTime.UtcNow,
                    RequestsPerMinute = 0,
                    RecentResults = new List<string> { $"Error: {ex.Message}" },
                    RateLimitInfo = "ALL companies processing failed"
                });
            }
        }

        // POST: Reviews/DeleteAllCompanies
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAllCompanies()
        {
            try
            {
                _logger.LogWarning("Admin user initiated DELETE ALL COMPANIES operation");

                // Get count for logging
                var totalCount = await _context.Companies.CountAsync();
                var totalReviews = await _context.Reviews.CountAsync();

                // Delete all reviews first (due to foreign key constraints)
                _context.Reviews.RemoveRange(_context.Reviews);

                // Delete all companies
                _context.Companies.RemoveRange(_context.Companies);

                await _context.SaveChangesAsync();

                _logger.LogWarning($"DELETE ALL COMPANIES completed: Removed {totalCount} companies and {totalReviews} reviews");

                TempData["Success"] = $"All companies and reviews have been deleted successfully. Removed {totalCount} companies and {totalReviews} reviews.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting all companies");
                TempData["Error"] = "An error occurred while deleting companies. Some data may not have been removed.";
            }

            return RedirectToAction(nameof(Index));
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

        // GET: Reviews/ViewReportResult
        [Authorize(Roles = "Admin")]
        public IActionResult ViewReportResult(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                TempData["Error"] = "Invalid session ID";
                return RedirectToAction(nameof(ReviewMonitor));
            }

            var report = _progressService.GetReport(sessionId);
            if (report == null)
            {
                TempData["Error"] = "Report not found or has expired";
                return RedirectToAction(nameof(ReviewMonitor));
            }

            // Clean up progress and report data after retrieval
            _progressService.ClearProgress(sessionId);
            _progressService.ClearReport(sessionId);

            return View("ReviewReport", report);
        }

        // POST: Reviews/GenerateReviewReport
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateReviewReport(DateTime fromDate, DateTime toDate, int maxRating = 3, string? notificationEmail = null, string? sendEmailAlert = null, string? selectedCompanyIds = null)
        {
            try
            {
                // Parse sendEmailAlert - checkbox will send "true" if checked, null if unchecked
                bool shouldSendEmail = !string.IsNullOrEmpty(sendEmailAlert) && sendEmailAlert.ToLower() == "true";

                _logger.LogInformation($"Starting review report generation. SendEmail: {shouldSendEmail}, Email: {notificationEmail}, SelectedCompanyIds: {selectedCompanyIds}");

                // Debug: Log all form values received
                _logger.LogInformation($"Form values received - sendEmailAlert raw: '{sendEmailAlert}', parsed: {shouldSendEmail}, notificationEmail: '{notificationEmail}', selectedCompanyIds: '{selectedCompanyIds}', fromDate: {fromDate}, toDate: {toDate}, maxRating: {maxRating}");

                List<Company> companies;

                // Filter companies based on selection
                if (!string.IsNullOrWhiteSpace(selectedCompanyIds))
                {
                    var companyIdList = selectedCompanyIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => id.Trim())
                        .Where(id => !string.IsNullOrEmpty(id))
                        .ToList();

                    _logger.LogInformation($"Parsed company IDs: [{string.Join(", ", companyIdList)}] from raw input: '{selectedCompanyIds}'");

                    if (companyIdList.Any())
                    {
                        companies = await _context.Companies
                            .Where(c => c.IsActive && !string.IsNullOrEmpty(c.PlaceId) && companyIdList.Contains(c.Id))
                            .ToListAsync();

                        _logger.LogInformation($"Filtered to {companies.Count} selected companies from {companyIdList.Count} requested IDs");
                    }
                    else
                    {
                        // Fallback to all companies if parsing failed
                        companies = await _context.Companies
                            .Where(c => c.IsActive && !string.IsNullOrEmpty(c.PlaceId))
                            .ToListAsync();
                    }
                }
                else
                {
                    // Get all active companies with Place IDs
                    companies = await _context.Companies
                        .Where(c => c.IsActive && !string.IsNullOrEmpty(c.PlaceId))
                        .ToListAsync();
                }

                var reportData = new List<CompanyReviewReport>();
                var apiErrors = new List<string>();

                foreach (var company in companies)
                {
                    try
                    {
                        // Get filtered reviews (already filtered by date and rating in the service)
                        var reviewData = await _googlePlacesService.GetFilteredReviewsAsync(
                            company, fromDate, toDate, null, maxRating);

                        if (reviewData == null)
                        {
                            var errorMsg = $"Failed to fetch reviews for '{company.Name}' (PlaceID: {company.PlaceId}) - API returned null";
                            _logger.LogError(errorMsg);
                            apiErrors.Add(errorMsg);
                            continue;
                        }

                        _logger.LogInformation($"Company '{company.Name}': Retrieved {reviewData.Reviews?.Count ?? 0} filtered reviews");

                        if (reviewData.Reviews?.Any() == true)
                        {
                            // Reviews are already filtered by the service, no need to filter again
                            var badReviews = reviewData.Reviews
                                .OrderBy(r => r.Rating)
                                .ThenByDescending(r => r.Time)
                                .ToList();

                            _logger.LogInformation($"Company '{company.Name}': Found {badReviews.Count} reviews with rating ≤ {maxRating}");

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
                        else
                        {
                            _logger.LogInformation($"Company '{company.Name}': No reviews found matching criteria (date: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}, maxRating: {maxRating})");
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMsg = $"Error fetching reviews for '{company.Name}' (PlaceID: {company.PlaceId}): {ex.Message}";
                        _logger.LogError(ex, errorMsg);
                        apiErrors.Add(errorMsg);
                        // Continue with other companies
                    }
                }

                // Add API errors to TempData to display in the report
                if (apiErrors.Any())
                {
                    TempData["ApiErrors"] = string.Join("<br/>", apiErrors);
                    _logger.LogWarning($"Report completed with {apiErrors.Count} API errors");
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

                // Send email alert if enabled and email is provided
                if (shouldSendEmail && !string.IsNullOrWhiteSpace(notificationEmail))
                {
                    _logger.LogInformation($"Attempting to send email to {notificationEmail}");

                    // Check email service configuration first
                    var isEmailConfigured = await _emailService.IsConfiguredAsync();
                    if (!isEmailConfigured)
                    {
                        _logger.LogError("Email service is not properly configured");
                        TempData["EmailWarning"] = "Email service is not configured. Check your email settings.";
                    }
                    else
                    {
                        try
                        {
                            // Generate Excel report
                            var excelData = _excelService.GenerateReviewReportExcel(report);
                            var excelFileName = $"Review_Report_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";

                            _logger.LogInformation($"Generated Excel report: {excelFileName}, Size: {excelData?.Length ?? 0} bytes, Report has {report.CompanyReports.Count} companies with {report.TotalBadReviews} total reviews");

                            // Send email with Excel attachment
                            var emailSent = await _emailService.SendReviewReportEmailWithExcelAsync(
                                notificationEmail,
                                report,
                                $"Manual Review Report ({fromDate:MMM dd} - {toDate:MMM dd})",
                                excelData,
                                excelFileName);

                            if (emailSent)
                            {
                                _logger.LogInformation($"Review monitor email with Excel attachment sent successfully to {notificationEmail}");
                                TempData["EmailSuccess"] = $"Review report email with Excel attachment sent successfully to {notificationEmail}";
                            }
                            else
                            {
                                _logger.LogWarning($"Failed to send review monitor email to {notificationEmail}");
                                TempData["EmailWarning"] = "Failed to send email alert. Check your email configuration and try again.";
                            }
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, $"Error sending review monitor email to {notificationEmail}");
                            TempData["EmailWarning"] = $"Error occurred while sending email: {emailEx.Message}";
                        }
                    }
                }
                else if (shouldSendEmail && string.IsNullOrWhiteSpace(notificationEmail))
                {
                    TempData["EmailWarning"] = "Email alert was enabled but no email address was provided.";
                }

                // Pass additional context to the view
                ViewBag.IsSelectiveReport = !string.IsNullOrWhiteSpace(selectedCompanyIds);
                ViewBag.SelectedCompanyCount = ViewBag.IsSelectiveReport ? companies.Count : 0;

                return View("ReviewReport", report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating review report");
                TempData["Error"] = "An error occurred while generating the review report.";
                return RedirectToAction(nameof(ReviewMonitor));
            }
        }

        // POST: Reviews/SendTestEmail
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendTestEmail(string testEmail)
        {
            if (string.IsNullOrWhiteSpace(testEmail))
            {
                TempData["EmailWarning"] = "Please provide an email address for the test.";
                return RedirectToAction(nameof(ReviewMonitor));
            }

            try
            {
                var isConfigured = await _emailService.IsConfiguredAsync();
                if (!isConfigured)
                {
                    TempData["EmailWarning"] = "Email service is not configured. Check your email settings.";
                    return RedirectToAction(nameof(ReviewMonitor));
                }

                var emailSent = await _emailService.SendTestEmailAsync(testEmail);

                if (emailSent)
                {
                    TempData["EmailSuccess"] = $"Test email sent successfully to {testEmail}";
                }
                else
                {
                    TempData["EmailWarning"] = "Failed to send test email. Check your email configuration.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending test email to {testEmail}");
                TempData["EmailWarning"] = $"Error sending test email: {ex.Message}";
            }

            return RedirectToAction(nameof(ReviewMonitor));
        }

        // POST: Reviews/ToggleCustomerStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleCustomerStatus(string id)
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
                // Toggle the customer status
                company.IsCurrentCustomer = !company.IsCurrentCustomer;
                company.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var statusText = company.IsCurrentCustomer ? "Current Customer" : "Potential Customer";
                TempData["Success"] = $"✓ {company.Name} has been marked as {statusText}";

                _logger.LogInformation($"Company {company.Name} customer status changed to: {statusText}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error toggling customer status for company {company.Name}");
                TempData["Error"] = "An error occurred while updating the customer status.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Reviews/BulkUpdateCustomerStatus
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpdateCustomerStatus(List<string> companyIds, bool isCurrentCustomer)
        {
            if (companyIds == null || !companyIds.Any())
            {
                TempData["Error"] = "No companies selected for transfer.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var companies = await _context.Companies
                    .Where(c => companyIds.Contains(c.Id))
                    .ToListAsync();

                if (!companies.Any())
                {
                    TempData["Error"] = "No companies found to transfer.";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var company in companies)
                {
                    company.IsCurrentCustomer = isCurrentCustomer;
                    company.LastUpdated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                var statusName = isCurrentCustomer ? "Current Customers" : "Potential Customers";
                TempData["Success"] = $"Successfully moved {companies.Count} companies to {statusName}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk customer status update");
                TempData["Error"] = "An error occurred while updating companies.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Reviews/BulkDeleteCompanies
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDeleteCompanies(List<string> companyIds)
        {
            if (companyIds == null || !companyIds.Any())
            {
                TempData["Error"] = "No companies selected for deletion.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var companies = await _context.Companies
                    .Include(c => c.Reviews)
                    .Where(c => companyIds.Contains(c.Id))
                    .ToListAsync();

                if (!companies.Any())
                {
                    TempData["Error"] = "No companies found to delete.";
                    return RedirectToAction(nameof(Index));
                }

                // Remove all associated reviews first (cascade delete should handle this, but being explicit)
                var allReviews = companies.SelectMany(c => c.Reviews).ToList();
                _context.Reviews.RemoveRange(allReviews);

                // Remove the companies
                _context.Companies.RemoveRange(companies);

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Successfully deleted {companies.Count} companies and their associated reviews.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk company deletion");
                TempData["Error"] = "An error occurred while deleting companies. Some companies may not have been deleted.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Reviews/BulkDeleteAllPotential
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDeleteAllPotential(string letter)
        {
            if (string.IsNullOrEmpty(letter) || letter.Length != 1)
            {
                TempData["Error"] = "Invalid letter specified.";
                return RedirectToAction(nameof(Index));
            }

            letter = letter.ToUpper();

            try
            {
                var potentialCompanies = await _context.Companies
                    .Include(c => c.Reviews)
                    .Where(c => !c.IsCurrentCustomer && c.Name.StartsWith(letter))
                    .ToListAsync();

                if (!potentialCompanies.Any())
                {
                    TempData["Info"] = $"No potential customers starting with '{letter}' found to delete.";
                    return RedirectToAction(nameof(Index), new { letter });
                }

                // Remove all associated reviews first (cascade delete should handle this, but being explicit)
                var allReviews = potentialCompanies.SelectMany(c => c.Reviews).ToList();
                _context.Reviews.RemoveRange(allReviews);

                // Remove the companies
                _context.Companies.RemoveRange(potentialCompanies);

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Successfully deleted {potentialCompanies.Count} potential customers starting with '{letter}' and their associated reviews.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during bulk deletion of potential customers starting with '{letter}'");
                TempData["Error"] = "An error occurred while deleting companies. Some companies may not have been deleted.";
            }

            return RedirectToAction(nameof(Index), new { letter });
        }

        // GET: Reviews/CleanupDuplicates
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CleanupDuplicates()
        {
            try
            {
                // Find all companies and group them by name (case-insensitive) in memory
                var allCompanies = await _context.Companies.ToListAsync();

                // First, try exact name matches (case-insensitive)
                var exactDuplicateGroups = allCompanies
                    .GroupBy(c => c.Name.ToLower().Trim())
                    .Where(g => g.Count() > 1)
                    .Select(g => new DuplicateGroup
                    {
                        Name = g.Key,
                        Companies = g.OrderBy(c => c.LastUpdated).ToList(),
                        Type = "Exact"
                    })
                    .ToList();

                // Then, find potential duplicates by Place ID
                var placeIdDuplicates = allCompanies
                    .Where(c => !string.IsNullOrEmpty(c.PlaceId))
                    .GroupBy(c => c.PlaceId.Trim())
                    .Where(g => g.Count() > 1)
                    .Select(g => new DuplicateGroup
                    {
                        Name = $"Place ID: {g.Key}",
                        Companies = g.OrderBy(c => c.LastUpdated).ToList(),
                        Type = "PlaceID"
                    })
                    .ToList();

                // Combine both types
                var allDuplicateGroups = new List<DuplicateGroup>();
                allDuplicateGroups.AddRange(exactDuplicateGroups);
                allDuplicateGroups.AddRange(placeIdDuplicates);

                // Add debug info
                ViewBag.TotalCompanies = allCompanies.Count;
                ViewBag.DuplicateGroups = allDuplicateGroups;
                ViewBag.ExactDuplicates = exactDuplicateGroups.Count;
                ViewBag.PlaceIdDuplicates = placeIdDuplicates.Count;

                if (!allDuplicateGroups.Any())
                {
                    ViewBag.TotalDuplicates = 0;
                    return View();
                }

                var totalDuplicates = allDuplicateGroups.Sum(g => g.Companies.Count - 1); // -1 because we keep one
                ViewBag.TotalDuplicates = totalDuplicates;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding duplicate companies");
                TempData["Error"] = "An error occurred while searching for duplicate companies.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Reviews/ExecuteCleanupDuplicates
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExecuteCleanupDuplicates()
        {
            try
            {
                // Find all companies and group them by name (case-insensitive) in memory
                var allCompanies = await _context.Companies
                    .Include(c => c.Reviews)
                    .ToListAsync();

                var duplicateGroups = allCompanies
                    .GroupBy(c => c.Name.ToLower().Trim())
                    .Where(g => g.Count() > 1)
                    .ToList();

                if (!duplicateGroups.Any())
                {
                    TempData["Info"] = "No duplicate companies found to clean up.";
                    return RedirectToAction(nameof(Index));
                }

                int deletedCount = 0;
                int mergedReviews = 0;

                foreach (var group in duplicateGroups)
                {
                    var companies = group.OrderBy(c => c.LastUpdated).ToList();
                    var keepCompany = companies.First(); // Keep the oldest one
                    var duplicatesToDelete = companies.Skip(1).ToList();

                    // Merge reviews from duplicates into the company we're keeping
                    foreach (var duplicate in duplicatesToDelete)
                    {
                        // Transfer reviews to the kept company
                        foreach (var review in duplicate.Reviews)
                        {
                            // Check if the review already exists in the kept company (by author and time)
                            var existingReview = keepCompany.Reviews.FirstOrDefault(r =>
                                r.AuthorName == review.AuthorName &&
                                Math.Abs((r.Time - review.Time).TotalMinutes) < 1);

                            if (existingReview == null)
                            {
                                review.CompanyId = keepCompany.Id;
                                mergedReviews++;
                            }
                            else
                            {
                                // Remove duplicate review
                                _context.Reviews.Remove(review);
                            }
                        }

                        // Update kept company with best available data
                        if (string.IsNullOrEmpty(keepCompany.PlaceId) && !string.IsNullOrEmpty(duplicate.PlaceId))
                        {
                            keepCompany.PlaceId = duplicate.PlaceId;
                        }
                        if (string.IsNullOrEmpty(keepCompany.GoogleMapsUrl) && !string.IsNullOrEmpty(duplicate.GoogleMapsUrl))
                        {
                            keepCompany.GoogleMapsUrl = duplicate.GoogleMapsUrl;
                        }

                        // Remove the duplicate company
                        _context.Companies.Remove(duplicate);
                        deletedCount++;
                    }

                    // Update the kept company's timestamp
                    keepCompany.LastUpdated = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Cleanup completed successfully! Deleted {deletedCount} duplicate companies and merged {mergedReviews} reviews.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing duplicate cleanup");
                TempData["Error"] = "An error occurred during cleanup. Some duplicates may not have been removed.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Helper method to parse relative time strings
        private static DateTime ParseRelativeTime(string relativeTime)
        {
            var now = DateTime.UtcNow;
            if (string.IsNullOrEmpty(relativeTime)) return now;

            var lowerTime = relativeTime.ToLower();

            try
            {
                // Handle "a [unit] ago" patterns
                if (System.Text.RegularExpressions.Regex.IsMatch(lowerTime, @"\b(a|an|en|ett)\s+(second|sekund|minute|minut|hour|timme|day|dag|week|vecka|month|månad|year|år)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                {
                    if (lowerTime.Contains("second") || lowerTime.Contains("sekund")) return now.AddSeconds(-1);
                    if (lowerTime.Contains("minute") || lowerTime.Contains("minut")) return now.AddMinutes(-1);
                    if (lowerTime.Contains("hour") || lowerTime.Contains("timme")) return now.AddHours(-1);
                    if (lowerTime.Contains("day") || lowerTime.Contains("dag")) return now.AddDays(-1);
                    if (lowerTime.Contains("week") || lowerTime.Contains("vecka")) return now.AddDays(-7);
                    if (lowerTime.Contains("month") || lowerTime.Contains("månad")) return now.AddMonths(-1);
                    if (lowerTime.Contains("year") || lowerTime.Contains("år")) return now.AddYears(-1);
                }

                // Pattern: "X [unit] ago"
                var match = System.Text.RegularExpressions.Regex.Match(lowerTime, @"(\d+)\s*(second|sekund|minute|minut|hour|timme|timmar|day|dag|dagar|week|vecka|veckor|month|månad|månader|year|år)s?\b", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    var value = int.Parse(match.Groups[1].Value);
                    var unit = match.Groups[2].Value.ToLower();

                    return unit switch
                    {
                        var u when u.Contains("second") || u.Contains("sekund") => now.AddSeconds(-value),
                        var u when u.Contains("minute") || u.Contains("minut") => now.AddMinutes(-value),
                        var u when u.Contains("hour") || u.Contains("timme") || u.Contains("timmar") => now.AddHours(-value),
                        var u when u.Contains("day") || u.Contains("dag") => now.AddDays(-value),
                        var u when u.Contains("week") || u.Contains("vecka") || u.Contains("veckor") => now.AddDays(-value * 7),
                        var u when u.Contains("month") || u.Contains("månad") || u.Contains("månader") => now.AddMonths(-value),
                        var u when u.Contains("year") || u.Contains("år") => now.AddYears(-value),
                        _ => now
                    };
                }
            }
            catch { }

            return now;
        }

        // GET: Reviews/EmailCustomers
        [Authorize(Roles = "Admin")]
        public IActionResult EmailCustomers()
        {
            return View();
        }

        // POST: Reviews/UploadCustomerList
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadCustomerList(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                return Json(new { success = false, error = "Please select an Excel file to upload." });
            }

            var extension = Path.GetExtension(excelFile.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                return Json(new { success = false, error = "Please upload a valid Excel file (.xlsx or .xls)" });
            }

            try
            {
                List<CustomerEmailData> customers;
                using (var stream = excelFile.OpenReadStream())
                {
                    customers = _excelService.ParseCustomerEmailDataFromExcel(stream);
                }

                if (!customers.Any())
                {
                    return Json(new { success = false, error = "No valid customer data found in the Excel file." });
                }

                // Store customers in TempData for the next request
                TempData["CustomerEmailData"] = System.Text.Json.JsonSerializer.Serialize(customers);

                return Json(new {
                    success = true,
                    customerCount = customers.Count,
                    customers = customers.Select(c => new {
                        companyName = c.CompanyName,
                        email = c.Email,
                        badReviewCount = c.BadReviewCount
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading customer list");
                return Json(new { success = false, error = $"Error parsing file: {ex.Message}" });
            }
        }

        // POST: Reviews/SendBatchCustomerEmails
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendBatchCustomerEmails(
            string senderName,
            string senderPhone,
            string senderWebsite,
            bool isTestMode = false,
            string? testEmail = null)
        {
            try
            {
                // Retrieve customer data from TempData
                var customerDataJson = TempData["CustomerEmailData"] as string;
                if (string.IsNullOrEmpty(customerDataJson))
                {
                    return Json(new { success = false, error = "Customer data not found. Please upload the Excel file again." });
                }

                var customers = System.Text.Json.JsonSerializer.Deserialize<List<CustomerEmailData>>(customerDataJson);
                if (customers == null || !customers.Any())
                {
                    return Json(new { success = false, error = "No customer data to process." });
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(senderName) || string.IsNullOrWhiteSpace(senderPhone) || string.IsNullOrWhiteSpace(senderWebsite))
                {
                    return Json(new { success = false, error = "Please provide sender name, phone, and website." });
                }

                if (isTestMode && string.IsNullOrWhiteSpace(testEmail))
                {
                    return Json(new { success = false, error = "Please provide a test email address for test mode." });
                }

                // Create session for progress tracking
                var sessionId = _progressService.CreateSession();

                // Start background email sending
                _ = Task.Run(() => SendBatchCustomerEmailsBackground(customers, senderName, senderPhone, senderWebsite, isTestMode, testEmail, sessionId));

                return Json(new { success = true, sessionId = sessionId, totalCustomers = customers.Count, isTestMode = isTestMode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting batch customer email send");
                return Json(new { success = false, error = "Failed to start email sending process." });
            }
        }

        // Background method for sending batch customer emails
        private async Task SendBatchCustomerEmailsBackground(
            List<CustomerEmailData> customers,
            string senderName,
            string senderPhone,
            string senderWebsite,
            bool isTestMode,
            string? testEmail,
            string sessionId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ReviewsController>>();

            try
            {
                var startTime = DateTime.UtcNow;
                int successCount = 0;
                int failCount = 0;

                logger.LogInformation($"Starting batch customer email send: {customers.Count} customers, Test Mode: {isTestMode}, Session: {sessionId}");

                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = customers.Count,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Starting email campaign...",
                    Status = "Initializing",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = false,
                    StartTime = startTime,
                    RequestsPerMinute = 0,
                    RateLimitInfo = isTestMode ? $"TEST MODE - All emails will be sent to {testEmail}" : "Sending to real customer emails"
                });

                for (int i = 0; i < customers.Count; i++)
                {
                    var customer = customers[i];
                    var currentTime = DateTime.UtcNow;
                    var elapsed = currentTime - startTime;
                    var avgTimePerItem = i > 0 ? elapsed.TotalSeconds / i : 0;
                    var remainingItems = customers.Count - i;
                    var estimatedRemaining = avgTimePerItem > 0 ? (int)(remainingItems * avgTimePerItem) : 0;

                    _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                    {
                        TotalItems = customers.Count,
                        ProcessedItems = i,
                        SuccessfulItems = successCount,
                        FailedItems = failCount,
                        CurrentItem = $"Sending to: {customer.CompanyName}",
                        Status = "Sending",
                        EstimatedRemainingSeconds = estimatedRemaining,
                        IsComplete = false,
                        StartTime = startTime,
                        RequestsPerMinute = i > 0 ? (i / elapsed.TotalMinutes) : 0,
                        RateLimitInfo = isTestMode ? $"TEST MODE - Sending to {testEmail}" : $"Sending to {customer.Email}"
                    });

                    try
                    {
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

                        var sent = await emailService.SendCustomerOutreachEmailAsync(emailToSend, senderName, senderPhone, senderWebsite);

                        if (sent)
                        {
                            successCount++;
                            logger.LogInformation($"✓ Email sent to {customer.CompanyName} ({customer.Email})");
                        }
                        else
                        {
                            failCount++;
                            logger.LogWarning($"✗ Failed to send email to {customer.CompanyName} ({customer.Email})");
                        }

                        // Small delay between emails
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        logger.LogError(ex, $"Error sending email to {customer.CompanyName} ({customer.Email})");
                    }
                }

                // Final progress update
                var finalElapsed = DateTime.UtcNow - startTime;
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = customers.Count,
                    ProcessedItems = customers.Count,
                    SuccessfulItems = successCount,
                    FailedItems = failCount,
                    CurrentItem = "Email campaign completed!",
                    Status = "Complete",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = startTime,
                    RequestsPerMinute = customers.Count / finalElapsed.TotalMinutes,
                    RateLimitInfo = $"Completed in {finalElapsed.TotalMinutes:F1} minutes. Success: {successCount}, Failed: {failCount}"
                });

                logger.LogInformation($"Batch customer email send completed: {successCount} sent, {failCount} failed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in batch customer email send session {sessionId}");
                _progressService.UpdateProgress(sessionId, new BatchProgressInfo
                {
                    TotalItems = customers.Count,
                    ProcessedItems = 0,
                    SuccessfulItems = 0,
                    FailedItems = 0,
                    CurrentItem = "Error occurred during email campaign",
                    Status = "Error",
                    EstimatedRemainingSeconds = 0,
                    IsComplete = true,
                    StartTime = DateTime.UtcNow,
                    RequestsPerMinute = 0,
                    RateLimitInfo = $"Error: {ex.Message}"
                });
            }
        }
    }

    public class DuplicateGroup
    {
        public string Name { get; set; } = "";
        public List<Company> Companies { get; set; } = new List<Company>();
        public string Type { get; set; } = "";
    }
}