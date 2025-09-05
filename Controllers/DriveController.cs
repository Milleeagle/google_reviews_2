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

        public DriveController(IGoogleDriveService googleDriveService, ApplicationDbContext context, ILogger<DriveController> logger)
        {
            _googleDriveService = googleDriveService;
            _context = context;
            _logger = logger;
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
        public async Task<IActionResult> ImportSelectedCompanies(string documentId, List<int> selectedRows)
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
                    _logger.LogInformation($"Processing company from row {sheetCompany.RowNumber}: Name='{sheetCompany.Name}', AlreadyExists={sheetCompany.AlreadyExists}");
                    
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
                        IsActive = true,
                        LastUpdated = DateTime.UtcNow
                    };

                    _logger.LogInformation($"Adding new company: '{company.Name}' with ID: {company.Id}");
                    _context.Companies.Add(company);
                    importedCount++;
                }

                if (importedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"Imported {importedCount} companies from sheet, skipped {skippedCount}");
                
                if (importedCount > 0)
                {
                    TempData["Success"] = $"Successfully imported {importedCount} companies. {skippedCount} were skipped (already exist or missing name).";
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
                // Find companies that either have no Place ID OR have invalid Place IDs (like hex values)
                var companies = await _context.Companies
                    .Where(c => !string.IsNullOrEmpty(c.GoogleMapsUrl) && 
                               (string.IsNullOrEmpty(c.PlaceId) || 
                                c.PlaceId.StartsWith("0x") || 
                                c.PlaceId.StartsWith("CID:") ||
                                !c.PlaceId.StartsWith("ChIJ")))
                    .ToListAsync();

                int updatedCount = 0;

                foreach (var company in companies)
                {
                    var placeId = ExtractPlaceIdFromUrl(company.GoogleMapsUrl);
                    if (!string.IsNullOrEmpty(placeId))
                    {
                        company.PlaceId = placeId;
                        company.LastUpdated = DateTime.UtcNow;
                        updatedCount++;
                        _logger.LogInformation($"Extracted Place ID '{placeId}' for company '{company.Name}'");
                    }
                }

                if (updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Successfully extracted Place IDs for {updatedCount} companies.";
                }
                else
                {
                    TempData["Info"] = "No Place IDs could be extracted from existing company URLs.";
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
                // Use the same improved logic as in GoogleDriveService
                
                // 1. Look for typical ChIJ Place ID pattern first (most reliable)
                var match = System.Text.RegularExpressions.Regex.Match(url, @"(ChIJ[a-zA-Z0-9\-_]{16,})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 2. Direct Place ID in URL parameter
                match = System.Text.RegularExpressions.Regex.Match(url, @"place_id=([a-zA-Z0-9\-_]+)");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 3. Look for Place ID after !1s (but validate it's actually a Place ID)
                match = System.Text.RegularExpressions.Regex.Match(url, @"!1s(ChIJ[a-zA-Z0-9\-_]{16,})");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                // 4. Look for Place ID after 1s (but be more selective)
                match = System.Text.RegularExpressions.Regex.Match(url, @"1s(ChIJ[a-zA-Z0-9\-_]{16,})");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                // 5. Try to extract from data parameter
                match = System.Text.RegularExpressions.Regex.Match(url, @"data=.*?(ChIJ[a-zA-Z0-9\-_]{16,})");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }

                return null;
            }
            catch
            {
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