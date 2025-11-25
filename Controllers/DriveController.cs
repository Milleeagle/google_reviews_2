using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using google_reviews.Services;
using google_reviews.Models;
using google_reviews.Data;

namespace google_reviews.Controllers
{
    [Authorize]
    public class DriveController : Controller
    {
        private readonly IGoogleDriveService _googleDriveService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DriveController> _logger;
        private readonly IGooglePlacesService _googlePlacesService;

        public DriveController(IGoogleDriveService googleDriveService, ApplicationDbContext context, ILogger<DriveController> logger, IGooglePlacesService googlePlacesService)
        {
            _googleDriveService = googleDriveService;
            _context = context;
            _logger = logger;
            _googlePlacesService = googlePlacesService;
        }

        // GET: Drive
        public async Task<IActionResult> Index()
        {
            var sharedDocuments = await _googleDriveService.ListSharedDocumentsAsync();
            return View(sharedDocuments);
        }

        // GET: Drive/Document/5
        public async Task<IActionResult> Document(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var document = await _googleDriveService.GetDocumentAsync(id);
            if (document == null)
            {
                TempData["Error"] = "Document not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            return View(document);
        }

        // POST: Drive/AccessDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AccessDocument(string documentUrl)
        {
            if (string.IsNullOrEmpty(documentUrl))
            {
                TempData["Error"] = "Please provide a document URL or ID.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var document = await _googleDriveService.GetDocumentByUrlAsync(documentUrl);
                if (document == null)
                {
                    TempData["Error"] = "Could not access the document. Please check the URL and ensure it's shared with the service account.";
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = $"Successfully accessed document: {document.Name}";
                return RedirectToAction(nameof(Document), new { id = document.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error accessing document from URL: {documentUrl}");
                TempData["Error"] = "An error occurred while accessing the document.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Drive/ImportFromSheet/documentId
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportFromSheet(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Document ID is required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var companies = await _googleDriveService.ParseCompaniesFromSheetAsync(id);
                if (!companies.Any())
                {
                    TempData["Error"] = "No companies found in the sheet or unable to parse the document.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.DocumentId = id;
                return View(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading companies from sheet {id}");
                TempData["Error"] = "An error occurred while loading companies from the sheet.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Drive/ImportSelectedCompanies
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportSelectedCompanies(string documentId, List<int> selectedRows, bool customerStatus = false)
        {
            if (string.IsNullOrEmpty(documentId) || !selectedRows.Any())
            {
                TempData["Error"] = "Please select at least one company to import.";
                return RedirectToAction(nameof(ImportFromSheet), new { id = documentId });
            }

            try
            {
                var allCompanies = await _googleDriveService.ParseCompaniesFromSheetAsync(documentId);
                var selectedCompanies = allCompanies.Where(c => selectedRows.Contains(c.RowNumber)).ToList();
                
                int importedCount = 0;
                int skippedCount = 0;

                foreach (var sheetCompany in selectedCompanies)
                {
                    _logger.LogInformation($"Processing company from row {sheetCompany.RowNumber}: Name='{sheetCompany.Name}', EmailAddress='{sheetCompany.EmailAddress}', AlreadyExists={sheetCompany.AlreadyExists}");
                    
                    if (sheetCompany.AlreadyExists)
                    {
                        _logger.LogInformation($"Skipping company '{sheetCompany.Name}' - already exists (ID: {sheetCompany.ExistingCompanyId})");
                        skippedCount++;
                        continue;
                    }

                    if (string.IsNullOrEmpty(sheetCompany.Name))
                    {
                        _logger.LogWarning($"Skipping row {sheetCompany.RowNumber} - empty or null company name");
                        skippedCount++;
                        continue;
                    }

                    var company = new Company
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = sheetCompany.Name,
                        PlaceId = sheetCompany.PlaceId,
                        GoogleMapsUrl = sheetCompany.GoogleMapsUrl,
                        EmailAddress = sheetCompany.EmailAddress,
                        IsActive = true,
                        IsCurrentCustomer = customerStatus,
                        LastUpdated = DateTime.UtcNow
                    };

                    _logger.LogInformation($"Adding new company: '{company.Name}' with ID: {company.Id}, Email: '{company.EmailAddress}'");
                    _context.Companies.Add(company);
                    importedCount++;
                }

                if (importedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"Imported {importedCount} companies from sheet, skipped {skippedCount}");
                
                var statusText = customerStatus ? "Current Customers" : "Potential Customers";

                if (importedCount > 0)
                {
                    TempData["Success"] = $"Successfully imported {importedCount} companies as {statusText}. {skippedCount} were skipped (already exist or missing name).";
                }
                else
                {
                    TempData["Warning"] = "No companies were imported. They may already exist or be missing required information.";
                }

                return RedirectToAction("Index", "Reviews");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing companies from sheet {documentId}");
                TempData["Error"] = "An error occurred while importing companies.";
                return RedirectToAction(nameof(ImportFromSheet), new { id = documentId });
            }
        }

        // GET: Drive/DebugSheet/documentId
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DebugSheet(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Document ID required");
            }

            try
            {
                _logger.LogInformation($"Debug: Starting sheet analysis for document: {id}");
                
                // First, try to get the document metadata
                var document = await _googleDriveService.GetDocumentAsync(id);
                ViewBag.Document = document;
                
                // Then try to get raw sheet data
                var sheetData = await _googleDriveService.GetSheetDataAsync(id);
                ViewBag.SheetData = sheetData;
                
                // Finally, try to parse companies
                var companies = await _googleDriveService.ParseCompaniesFromSheetAsync(id);
                ViewBag.Companies = companies;

                _logger.LogInformation($"Debug: Document found: {document != null}, SheetData found: {sheetData != null}, Companies parsed: {companies?.Count ?? 0}");
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Debug error for sheet {id}");
                ViewBag.Error = ex.Message;
                ViewBag.StackTrace = ex.StackTrace;
                return View();
            }
        }

        // POST: Drive/ExtractPlaceIds
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExtractPlaceIds()
        {
            try
            {
                // First, test the Google Places API connection
                var apiTestResult = await _googlePlacesService.TestApiConnectionAsync();
                if (!apiTestResult)
                {
                    _logger.LogError("Google Places API connection test failed. Check API key configuration.");
                    TempData["Error"] = "Google Places API connection failed. Please check your API key configuration in appsettings.json.";
                    return RedirectToAction("Index", "Reviews");
                }

                _logger.LogInformation("Google Places API connection test passed.");

                // Find companies that have Google Maps URLs but NO Place ID (null or empty)
                var companies = await _context.Companies
                    .Where(c => !string.IsNullOrEmpty(c.GoogleMapsUrl) &&
                               string.IsNullOrEmpty(c.PlaceId))
                    .ToListAsync();

                int updatedCount = 0;
                int failedCount = 0;
                int cidUrlCount = 0;
                int duplicateCount = 0;
                var failedCompanies = new List<string>();
                var extractedPlaceIds = new HashSet<string>(); // Track Place IDs we've extracted in this session
                var existingPlaceIds = new HashSet<string>(); // Track existing Place IDs in database

                // Get all existing Place IDs to avoid duplicates
                var existingPlaceIdsList = await _context.Companies
                    .Where(c => !string.IsNullOrEmpty(c.PlaceId))
                    .Select(c => c.PlaceId)
                    .ToListAsync();

                foreach (var placeId in existingPlaceIdsList)
                {
                    if (!string.IsNullOrEmpty(placeId))
                    {
                        existingPlaceIds.Add(placeId);
                    }
                }

                _logger.LogInformation($"Starting Place ID extraction for {companies.Count} companies. Found {existingPlaceIds.Count} existing Place IDs to avoid duplicates.");

                if (companies.Count == 0)
                {
                    _logger.LogInformation("No companies found that need Place ID extraction. All companies either have valid Place IDs or are missing Google Maps URLs.");
                }

                foreach (var company in companies)
                {
                    _logger.LogInformation($"Processing company '{company.Name}' with URL: {company.GoogleMapsUrl}, Current PlaceId: '{company.PlaceId}'");

                    var placeId = ExtractPlaceIdFromUrl(company.GoogleMapsUrl);
                    if (!string.IsNullOrEmpty(placeId))
                    {
                        // Check for duplicates
                        if (existingPlaceIds.Contains(placeId) || extractedPlaceIds.Contains(placeId))
                        {
                            duplicateCount++;
                            _logger.LogWarning($"⚠️ Duplicate Place ID '{placeId}' found for company '{company.Name}' - skipping to avoid database constraint violation");
                            continue;
                        }

                        company.PlaceId = placeId;
                        company.LastUpdated = DateTime.UtcNow;
                        extractedPlaceIds.Add(placeId); // Track this Place ID to avoid duplicates within this session
                        updatedCount++;
                        _logger.LogInformation($"✓ Extracted Place ID '{placeId}' for company '{company.Name}'");
                    }
                    else
                    {
                        // Try to search for Place ID using Google Places Search API
                        _logger.LogInformation($"Attempting to search for Place ID for '{company.Name}' using Google Places Search API");

                        try
                        {
                            var searchedPlaceId = await _googlePlacesService.SearchPlaceByNameAsync(company.Name);
                            if (!string.IsNullOrEmpty(searchedPlaceId))
                            {
                                // Check for duplicates
                                if (existingPlaceIds.Contains(searchedPlaceId) || extractedPlaceIds.Contains(searchedPlaceId))
                                {
                                    duplicateCount++;
                                    _logger.LogWarning($"⚠️ Duplicate Place ID '{searchedPlaceId}' found via search for company '{company.Name}' - skipping to avoid database constraint violation");
                                    continue;
                                }

                                company.PlaceId = searchedPlaceId;
                                company.LastUpdated = DateTime.UtcNow;
                                extractedPlaceIds.Add(searchedPlaceId); // Track this Place ID to avoid duplicates within this session
                                updatedCount++;
                                _logger.LogInformation($"✓ Found Place ID '{searchedPlaceId}' via search for company '{company.Name}'");
                                continue; // Skip the failure handling below
                            }
                            else
                            {
                                _logger.LogWarning($"Google Places Search API returned null/empty result for '{company.Name}'");
                            }
                        }
                        catch (Exception searchEx)
                        {
                            _logger.LogError(searchEx, $"Error searching for Place ID for '{company.Name}': {searchEx.Message}");
                        }

                        failedCount++;
                        failedCompanies.Add(company.Name);

                        // Check if it's a CID-based URL
                        if (company.GoogleMapsUrl?.Contains("0x") == true || company.GoogleMapsUrl?.Contains("cid=") == true)
                        {
                            cidUrlCount++;
                            _logger.LogWarning($"✗ Company '{company.Name}' uses CID-based URL and search failed: {company.GoogleMapsUrl}");
                        }
                        else
                        {
                            _logger.LogWarning($"✗ Could not extract or find Place ID for company '{company.Name}' from URL: {company.GoogleMapsUrl}");
                        }
                    }
                }

                if (updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    var message = $"Successfully found Place IDs for {updatedCount} companies (via URL extraction and Google Places Search).";
                    if (duplicateCount > 0 || failedCount > 0)
                    {
                        var additionalInfo = new List<string>();

                        if (duplicateCount > 0)
                        {
                            additionalInfo.Add($"{duplicateCount} duplicates skipped");
                        }

                        if (failedCount > 0)
                        {
                            var failedInfo = $"{failedCount} could not be processed";
                            if (cidUrlCount > 0)
                            {
                                failedInfo += $" ({cidUrlCount} have CID-based URLs)";
                            }
                            additionalInfo.Add(failedInfo);
                        }

                        message += $" {string.Join(", ", additionalInfo)}.";
                    }
                    TempData["Success"] = message;
                    _logger.LogInformation($"Place ID extraction completed: {updatedCount} successful, {duplicateCount} duplicates skipped, {failedCount} failed ({cidUrlCount} CID-based)");
                }
                else
                {
                    var message = "No Place IDs could be found.";
                    var statusDetails = new List<string>();

                    if (duplicateCount > 0)
                    {
                        statusDetails.Add($"{duplicateCount} duplicates skipped");
                    }

                    if (cidUrlCount > 0)
                    {
                        statusDetails.Add($"{cidUrlCount} companies use CID-based URLs and couldn't be resolved");
                    }
                    else if (failedCount > 0)
                    {
                        statusDetails.Add($"{failedCount} companies have invalid URL formats or couldn't be found via search");
                    }

                    if (statusDetails.Any())
                    {
                        message += $" {string.Join(", ", statusDetails)}.";
                        TempData["Warning"] = message;
                    }
                    else
                    {
                        TempData["Info"] = "No companies found that need Place ID extraction.";
                    }
                }

                return RedirectToAction("Index", "Reviews");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting Place IDs from existing companies");
                TempData["Error"] = "An error occurred while extracting Place IDs.";
                return RedirectToAction("Index", "Reviews");
            }
        }

        private static string? ExtractPlaceIdFromUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
                // Modern comprehensive Google Maps URL Place ID extraction
                // Handles various URL formats from 2020-2024+

                // 1. Direct place_id parameter (most reliable when present)
                var match = System.Text.RegularExpressions.Regex.Match(url, @"place_id=([a-zA-Z0-9\-_]{20,})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 2. ChIJ Place ID anywhere in URL (most common format)
                match = System.Text.RegularExpressions.Regex.Match(url, @"(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 3. Modern !1s format (2023+ URLs)
                match = System.Text.RegularExpressions.Regex.Match(url, @"!1s(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 4. !4m format (common in shared URLs)
                match = System.Text.RegularExpressions.Regex.Match(url, @"!4m.*?!1s(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 5. !3m format (another variant)
                match = System.Text.RegularExpressions.Regex.Match(url, @"!3m.*?!1s(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 6. Data parameter extraction (encoded URLs)
                match = System.Text.RegularExpressions.Regex.Match(url, @"data=.*?(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 7. URL path format /place/ChIJ...
                match = System.Text.RegularExpressions.Regex.Match(url, @"/place/(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 8. Embedded format in coordinates (!2d!3d format)
                match = System.Text.RegularExpressions.Regex.Match(url, @"!2d[0-9.-]+!3d[0-9.-]+.*?(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 9. Try URL decoding first for encoded URLs
                try
                {
                    var decodedUrl = System.Web.HttpUtility.UrlDecode(url);
                    if (decodedUrl != url) // Only recurse if URL was actually encoded
                    {
                        var decodedResult = ExtractPlaceIdFromUrl(decodedUrl);
                        if (!string.IsNullOrEmpty(decodedResult))
                            return decodedResult;
                    }
                }
                catch
                {
                    // URL decoding failed, continue with other methods
                }

                // 10. Look for hex CID format (0x....:0x....)
                // Note: These are NOT proper Place IDs but can sometimes be used
                match = System.Text.RegularExpressions.Regex.Match(url, @"0x[a-fA-F0-9]+:0x([a-fA-F0-9]+)");
                if (match.Success)
                {
                    // Extract the second hex value and convert to decimal, then back to ChIJ format
                    // This is a fallback method that may not always work
                    var hexValue = match.Groups[1].Value;
                    if (TryConvertHexCidToPlaceId(hexValue, out string? placeId))
                    {
                        return placeId;
                    }
                }

                // 11. Look for direct CID parameter
                match = System.Text.RegularExpressions.Regex.Match(url, @"cid=([0-9]+)");
                if (match.Success)
                {
                    var cid = match.Groups[1].Value;
                    if (TryConvertCidToPlaceId(cid, out string? placeId))
                    {
                        return placeId;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error extracting Place ID from URL: {ex.Message}");
                return null;
            }
        }

        private static bool IsValidPlaceId(string placeId)
        {
            if (string.IsNullOrEmpty(placeId))
                return false;

            return placeId.StartsWith("ChIJ") &&
                   placeId.Length >= 20 &&
                   placeId.Length <= 35 &&
                   !placeId.StartsWith("0x");
        }

        private static bool TryConvertHexCidToPlaceId(string hexValue, out string? placeId)
        {
            placeId = null;
            try
            {
                // Convert hex to long, then try to use Google's conversion algorithm
                var cidLong = Convert.ToInt64(hexValue, 16);
                return TryConvertCidToPlaceId(cidLong.ToString(), out placeId);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryConvertCidToPlaceId(string cid, out string? placeId)
        {
            placeId = null;

            // CID to Place ID conversion is complex and not officially documented
            // For now, we'll return null to indicate we need to use alternative methods
            // In a production system, you might want to:
            // 1. Use Google Places API search with the business name
            // 2. Store CID values and convert them via other APIs
            // 3. Use third-party conversion services

            return false;
        }

        // GET: Drive/DebugPlaceId/companyId
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DebugPlaceId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Company ID required");
            }

            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company == null)
            {
                return NotFound("Company not found");
            }

            var extractedPlaceId = ExtractPlaceIdFromUrl(company.GoogleMapsUrl);
            
            ViewBag.Company = company;
            ViewBag.ExtractedPlaceId = extractedPlaceId;
            ViewBag.IsValidExtracted = !string.IsNullOrEmpty(extractedPlaceId) && IsValidPlaceId(extractedPlaceId);
            
            return View();
        }

        // POST: Drive/ClearInvalidPlaceId/companyId
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ClearInvalidPlaceId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                TempData["Error"] = "Company ID is required.";
                return RedirectToAction("Index", "Reviews");
            }

            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == id);
                if (company == null)
                {
                    TempData["Error"] = "Company not found.";
                    return RedirectToAction("Index", "Reviews");
                }

                // Clear the invalid Place ID
                company.PlaceId = null;
                company.LastUpdated = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Cleared invalid Place ID for company '{company.Name}'");
                TempData["Success"] = $"Cleared invalid Place ID for '{company.Name}'. You can now try to extract a new one.";
                
                return RedirectToAction("Index", "Reviews");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error clearing Place ID for company {id}");
                TempData["Error"] = "An error occurred while clearing the Place ID.";
                return RedirectToAction("Index", "Reviews");
            }
        }

        // GET: Drive/PlaceIdFinder
        [Authorize(Roles = "Admin")]
        public IActionResult PlaceIdFinder()
        {
            return View();
        }

        // GET: Drive/SimplePlaceIdFinder
        [Authorize(Roles = "Admin")]
        public IActionResult SimplePlaceIdFinder()
        {
            return View();
        }

        // POST: Drive/UpdateCompanyPlaceId
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCompanyPlaceId([FromBody] UpdatePlaceIdRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.CompanyId) || string.IsNullOrEmpty(request.PlaceId))
                {
                    return Json(new { success = false, message = "Company ID and Place ID are required." });
                }

                var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId);
                if (company == null)
                {
                    return Json(new { success = false, message = "Company not found." });
                }

                // Validate that it's a proper Place ID
                if (!IsValidPlaceId(request.PlaceId))
                {
                    return Json(new { success = false, message = "Invalid Place ID format." });
                }

                company.PlaceId = request.PlaceId;
                company.LastUpdated = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Updated Place ID for company '{company.Name}' to '{request.PlaceId}'");
                
                return Json(new { success = true, message = $"Place ID updated for {company.Name}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company Place ID");
                return Json(new { success = false, message = "An error occurred while updating the Place ID." });
            }
        }

        // POST: Drive/BulkImport
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkImport(string bulkData, bool customerStatus = false, bool skipExisting = true)
        {
            if (string.IsNullOrWhiteSpace(bulkData))
            {
                TempData["Error"] = "Please provide company data to import.";
                return RedirectToAction("Index");
            }

            var importResults = new List<string>();
            var errors = new List<string>();
            var skippedCount = 0;
            var importedCount = 0;

            try
            {
                // Split the data into lines
                var lines = bulkData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // Try different delimiters: tab, comma, semicolon
                    string[] parts = null;
                    if (line.Contains('\t'))
                    {
                        parts = line.Split('\t');
                    }
                    else if (line.Contains(','))
                    {
                        parts = line.Split(',');
                    }
                    else if (line.Contains(';'))
                    {
                        parts = line.Split(';');
                    }
                    else
                    {
                        errors.Add($"Could not parse line (no valid delimiter found): {line}");
                        continue;
                    }

                    // Clean up parts
                    parts = parts.Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray();

                    if (parts.Length < 2)
                    {
                        errors.Add($"Line must contain at least 2 columns (Company Name, Email/Place ID): {line}");
                        continue;
                    }

                    var companyName = parts[0];
                    var emailOrPlaceId = parts[1];
                    var placeId = parts.Length > 2 ? parts[2] : null;

                    string? emailAddress = null;

                    // If we don't have a separate place ID, check if the second column is a Place ID
                    if (string.IsNullOrEmpty(placeId) && emailOrPlaceId.StartsWith("ChIJ"))
                    {
                        placeId = emailOrPlaceId;
                        emailAddress = null; // Clear email since it's actually a Place ID
                    }
                    else
                    {
                        // Second column is an email address
                        emailAddress = emailOrPlaceId;
                    }

                    // Validate company name
                    if (string.IsNullOrWhiteSpace(companyName))
                    {
                        errors.Add($"Company name is required: {line}");
                        continue;
                    }

                    // Check if company already exists (by Place ID if available, otherwise by name)
                    Company? existingCompany = null;
                    if (!string.IsNullOrEmpty(placeId))
                    {
                        existingCompany = await _context.Companies.FirstOrDefaultAsync(c => c.PlaceId == placeId);
                    }
                    else
                    {
                        existingCompany = await _context.Companies.FirstOrDefaultAsync(c => c.Name == companyName);
                    }

                    if (existingCompany != null && skipExisting)
                    {
                        skippedCount++;
                        continue;
                    }

                    // Create or update company
                    if (existingCompany != null)
                    {
                        // Update existing company
                        existingCompany.IsCurrentCustomer = customerStatus;
                        existingCompany.LastUpdated = DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(placeId) && string.IsNullOrEmpty(existingCompany.PlaceId))
                        {
                            existingCompany.PlaceId = placeId;
                        }
                        if (!string.IsNullOrEmpty(emailAddress) && string.IsNullOrEmpty(existingCompany.EmailAddress))
                        {
                            existingCompany.EmailAddress = emailAddress;
                        }
                        importedCount++;
                    }
                    else
                    {
                        // Create new company
                        var newCompany = new Company
                        {
                            Name = companyName,
                            PlaceId = placeId ?? "",
                            EmailAddress = emailAddress,
                            IsCurrentCustomer = customerStatus,
                            IsActive = true,
                            LastUpdated = DateTime.UtcNow
                        };

                        _context.Companies.Add(newCompany);
                        _logger.LogInformation($"Bulk Import: Added company '{companyName}' with email '{emailAddress}' and Place ID '{placeId}'");
                        importedCount++;
                    }
                }

                // Save all changes
                if (importedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                // Prepare success message
                var successMessages = new List<string>();
                if (importedCount > 0)
                {
                    successMessages.Add($"Successfully imported {importedCount} companies");
                }
                if (skippedCount > 0)
                {
                    successMessages.Add($"Skipped {skippedCount} existing companies");
                }

                if (successMessages.Any())
                {
                    TempData["Success"] = string.Join(". ", successMessages) + ".";
                }

                // Add errors if any
                if (errors.Any())
                {
                    TempData["Warning"] = $"Import completed with {errors.Count} errors: " + string.Join("; ", errors.Take(5));
                    if (errors.Count > 5)
                    {
                        TempData["Warning"] += $" and {errors.Count - 5} more errors.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk import");
                TempData["Error"] = "An error occurred during bulk import. Please check your data format and try again.";
            }

            return RedirectToAction("Index");
        }

        // GET: Drive/TestConnection
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TestConnection()
        {
            var isConnected = await _googleDriveService.TestDriveConnectionAsync();
            ViewBag.DriveConnected = isConnected;
            
            return View();
        }
    }

    public class UpdatePlaceIdRequest
    {
        public string CompanyId { get; set; } = "";
        public string PlaceId { get; set; } = "";
    }
}