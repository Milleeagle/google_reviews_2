using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using google_reviews.Models;
using google_reviews.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace google_reviews.Services
{
    public class GoogleDriveService : IGoogleDriveService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleDriveService> _logger;
        private readonly ApplicationDbContext _context;
        private DriveService? _driveService;
        private SheetsService? _sheetsService;

        public GoogleDriveService(IConfiguration configuration, ILogger<GoogleDriveService> logger, ApplicationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        private async Task<DriveService?> GetDriveServiceAsync()
        {
            if (_driveService != null)
                return _driveService;

            try
            {
                // Try to get service account credentials from configuration
                var serviceAccountKeyPath = _configuration["GoogleDrive:ServiceAccountKeyPath"];
                var serviceAccountKeyJson = _configuration["GoogleDrive:ServiceAccountKeyJson"];

                GoogleCredential credential;

                if (!string.IsNullOrEmpty(serviceAccountKeyPath) && System.IO.File.Exists(serviceAccountKeyPath))
                {
                    // Load from file
                    credential = await CreateGoogleCredentialAsync(serviceAccountKeyPath, isFilePath: true, DriveService.Scope.DriveReadonly);
                }
                else if (!string.IsNullOrEmpty(serviceAccountKeyJson))
                {
                    // Load from JSON string (stored in user secrets or appsettings)
                    credential = await CreateGoogleCredentialAsync(serviceAccountKeyJson, isFilePath: false, DriveService.Scope.DriveReadonly);
                }
                else
                {
                    _logger.LogError("Google Drive service account credentials not configured. Set either GoogleDrive:ServiceAccountKeyPath or GoogleDrive:ServiceAccountKeyJson");
                    return null;
                }

                if (credential == null)
                {
                    _logger.LogError("Failed to create Google credential");
                    return null;
                }

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Google Reviews App"
                });

                return _driveService;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Google Drive service");
                return null;
            }
        }

        public async Task<GoogleDriveDocument?> GetDocumentAsync(string documentId)
        {
            var service = await GetDriveServiceAsync();
            if (service == null)
                return null;

            try
            {
                _logger.LogInformation($"Fetching document with ID: {documentId}");

                // Get file metadata
                var request = service.Files.Get(documentId);
                request.Fields = "id,name,mimeType,description,createdTime,modifiedTime,webViewLink,webContentLink,size,owners,shared";
                
                var file = await request.ExecuteAsync();

                var document = new GoogleDriveDocument
                {
                    Id = file.Id,
                    Name = file.Name,
                    MimeType = file.MimeType,
                    Description = file.Description,
                    CreatedTime = file.CreatedTime ?? DateTime.MinValue,
                    ModifiedTime = file.ModifiedTime ?? DateTime.MinValue,
                    WebViewLink = file.WebViewLink,
                    WebContentLink = file.WebContentLink,
                    Size = file.Size ?? 0,
                    Owners = file.Owners?.Select(o => o.DisplayName).ToList() ?? new List<string>(),
                    Shared = file.Shared ?? false
                };

                // Try to get content for text-based documents
                if (IsTextDocument(file.MimeType))
                {
                    document.Content = await GetDocumentContentAsync(service, documentId, file.MimeType);
                }

                _logger.LogInformation($"Successfully retrieved document: {file.Name}");
                return document;
            }
            catch (Google.GoogleApiException ex)
            {
                _logger.LogError(ex, $"Google API error retrieving document {documentId}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving document {documentId}");
                return null;
            }
        }

        public async Task<GoogleDriveDocument?> GetDocumentByUrlAsync(string shareUrl)
        {
            var documentId = ExtractDocumentIdFromUrl(shareUrl);
            if (string.IsNullOrEmpty(documentId))
            {
                _logger.LogWarning($"Could not extract document ID from URL: {shareUrl}");
                return null;
            }

            return await GetDocumentAsync(documentId);
        }

        public async Task<List<GoogleDriveDocument>> ListSharedDocumentsAsync()
        {
            var service = await GetDriveServiceAsync();
            if (service == null)
                return new List<GoogleDriveDocument>();

            try
            {
                _logger.LogInformation("Listing shared documents");

                var request = service.Files.List();
                request.Q = "sharedWithMe = true";
                request.Fields = "files(id,name,mimeType,description,createdTime,modifiedTime,webViewLink,webContentLink,size,owners,shared)";
                request.PageSize = 100;

                var response = await request.ExecuteAsync();
                var documents = new List<GoogleDriveDocument>();

                foreach (var file in response.Files)
                {
                    documents.Add(new GoogleDriveDocument
                    {
                        Id = file.Id,
                        Name = file.Name,
                        MimeType = file.MimeType,
                        Description = file.Description,
                        CreatedTime = file.CreatedTime ?? DateTime.MinValue,
                        ModifiedTime = file.ModifiedTime ?? DateTime.MinValue,
                        WebViewLink = file.WebViewLink,
                        WebContentLink = file.WebContentLink,
                        Size = file.Size ?? 0,
                        Owners = file.Owners?.Select(o => o.DisplayName).ToList() ?? new List<string>(),
                        Shared = file.Shared ?? false
                    });
                }

                _logger.LogInformation($"Found {documents.Count} shared documents");
                return documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing shared documents");
                return new List<GoogleDriveDocument>();
            }
        }

        public async Task<bool> TestDriveConnectionAsync()
        {
            var service = await GetDriveServiceAsync();
            if (service == null)
                return false;

            try
            {
                _logger.LogInformation("Testing Google Drive API connection");

                var request = service.About.Get();
                request.Fields = "user";
                var about = await request.ExecuteAsync();

                _logger.LogInformation($"Connected to Google Drive as: {about.User?.DisplayName ?? "Unknown"}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Google Drive API");
                return false;
            }
        }

        private async Task<string?> GetDocumentContentAsync(DriveService service, string fileId, string mimeType)
        {
            try
            {
                // For Google Docs, Sheets, Slides - export as plain text or HTML
                if (mimeType.StartsWith("application/vnd.google-apps."))
                {
                    var exportRequest = service.Files.Export(fileId, "text/plain");
                    using var stream = new MemoryStream();
                    await exportRequest.DownloadAsync(stream);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }
                // For other text files, download directly
                else if (IsTextDocument(mimeType))
                {
                    var downloadRequest = service.Files.Get(fileId);
                    using var stream = new MemoryStream();
                    await downloadRequest.DownloadAsync(stream);
                    return Encoding.UTF8.GetString(stream.ToArray());
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not retrieve content for file {fileId}");
                return null;
            }
        }

        private static bool IsTextDocument(string mimeType)
        {
            return mimeType.StartsWith("text/") ||
                   mimeType == "application/vnd.google-apps.document" ||
                   mimeType == "application/vnd.google-apps.spreadsheet" ||
                   mimeType == "application/json" ||
                   mimeType == "application/xml";
        }

        private static string? ExtractDocumentIdFromUrl(string url)
        {
            // Handle various Google Drive URL formats
            var patterns = new[]
            {
                @"\/d\/([a-zA-Z0-9-_]+)", // /d/document_id
                @"id=([a-zA-Z0-9-_]+)",   // id=document_id
                @"\/file\/d\/([a-zA-Z0-9-_]+)" // /file/d/document_id
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(url, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            // If no pattern matches, maybe it's just the ID itself
            if (Regex.IsMatch(url, @"^[a-zA-Z0-9-_]+$"))
            {
                return url;
            }

            return null;
        }

        private async Task<SheetsService?> GetSheetsServiceAsync()
        {
            if (_sheetsService != null)
                return _sheetsService;

            try
            {
                var serviceAccountKeyJson = _configuration["GoogleDrive:ServiceAccountKeyJson"];
                
                if (string.IsNullOrEmpty(serviceAccountKeyJson))
                {
                    _logger.LogError("Google Drive service account credentials not configured for Sheets");
                    return null;
                }

                var credential = await CreateGoogleCredentialAsync(serviceAccountKeyJson, isFilePath: false, scopes: SheetsService.Scope.SpreadsheetsReadonly);
                
                if (credential == null)
                {
                    _logger.LogError("Failed to create Google credential for Sheets");
                    return null;
                }

                _sheetsService = new SheetsService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "Google Reviews App"
                });

                return _sheetsService;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Google Sheets service");
                return null;
            }
        }

        public async Task<GoogleSheetData?> GetSheetDataAsync(string documentId, string? sheetName = null)
        {
            var service = await GetSheetsServiceAsync();
            if (service == null)
            {
                _logger.LogError("Google Sheets service is null - authentication issue");
                return null;
            }

            try
            {
                _logger.LogInformation($"Fetching sheet data from document: {documentId}");

                // If no sheet name provided, get the first sheet
                string range = string.IsNullOrEmpty(sheetName) ? "A:Z" : $"{sheetName}!A:Z";
                _logger.LogInformation($"Using range: {range}");
                
                var request = service.Spreadsheets.Values.Get(documentId, range);
                _logger.LogInformation("Executing Sheets API request...");
                
                var response = await request.ExecuteAsync();
                _logger.LogInformation($"API response received. Values count: {response.Values?.Count ?? 0}");

                if (response.Values == null || !response.Values.Any())
                {
                    _logger.LogWarning("No data found in the sheet - response.Values is null or empty");
                    
                    // Try to get sheet metadata to see if the sheet exists
                    try
                    {
                        var metadataRequest = service.Spreadsheets.Get(documentId);
                        var metadata = await metadataRequest.ExecuteAsync();
                        _logger.LogInformation($"Sheet metadata: Title={metadata.Properties?.Title}, Sheet count={metadata.Sheets?.Count}");
                        
                        if (metadata.Sheets?.Any() == true)
                        {
                            foreach (var sheet in metadata.Sheets)
                            {
                                _logger.LogInformation($"Available sheet: {sheet.Properties?.Title} (ID: {sheet.Properties?.SheetId})");
                            }
                        }
                    }
                    catch (Exception metaEx)
                    {
                        _logger.LogError(metaEx, "Error getting sheet metadata");
                    }
                    
                    return null;
                }

                var sheetData = new GoogleSheetData
                {
                    DocumentId = documentId,
                    SheetName = sheetName ?? "Sheet1",
                    TotalRows = response.Values.Count
                };

                // First row as headers
                if (response.Values.Count > 0)
                {
                    sheetData.Headers = response.Values[0].Cast<string>().ToList();
                    sheetData.TotalColumns = sheetData.Headers.Count;
                    _logger.LogInformation($"Headers found: {string.Join(", ", sheetData.Headers)}");
                }

                // Remaining rows as data
                if (response.Values.Count > 1)
                {
                    sheetData.Rows = response.Values.Skip(1)
                        .Select(row => row.Cast<string>().ToList())
                        .ToList();
                    _logger.LogInformation($"Data rows: {sheetData.Rows.Count}");
                }

                _logger.LogInformation($"Successfully retrieved sheet with {sheetData.TotalRows} rows and {sheetData.TotalColumns} columns");
                return sheetData;
            }
            catch (Google.GoogleApiException apiEx)
            {
                _logger.LogError(apiEx, $"Google API error fetching sheet data from document {documentId}: {apiEx.Message}. Error code: {apiEx.Error?.Code}");
                
                if (apiEx.Error?.Code == 403)
                {
                    _logger.LogError("Permission denied - sheet might not be shared with service account");
                }
                else if (apiEx.Error?.Code == 404)
                {
                    _logger.LogError("Sheet not found - check document ID");
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching sheet data from document {documentId}");
                return null;
            }
        }

        public async Task<List<SheetCompany>> ParseCompaniesFromSheetAsync(string documentId, string? sheetName = null)
        {
            var sheetData = await GetSheetDataAsync(documentId, sheetName);
            if (sheetData == null)
                return new List<SheetCompany>();

            var companies = new List<SheetCompany>();
            var existingCompanies = await _context.Companies.ToListAsync();

            for (int i = 0; i < sheetData.Rows.Count; i++)
            {
                var row = sheetData.Rows[i];
                var company = ParseCompanyFromRow(row, sheetData.Headers, i + 2); // +2 because row 1 is headers, array is 0-based

                // Check if company already exists
                var existing = existingCompanies.FirstOrDefault(c => 
                    c.Name.Equals(company.Name, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(company.PlaceId) && c.PlaceId == company.PlaceId));

                if (existing != null)
                {
                    company.AlreadyExists = true;
                    company.ExistingCompanyId = existing.Id;
                }

                companies.Add(company);
            }

            _logger.LogInformation($"Parsed {companies.Count} companies from sheet, {companies.Count(c => c.AlreadyExists)} already exist");
            return companies;
        }

        private SheetCompany ParseCompanyFromRow(List<string> row, List<string> headers, int rowNumber)
        {
            var company = new SheetCompany { RowNumber = rowNumber };
            _logger.LogDebug($"Parsing row {rowNumber} with {row.Count} values and {headers.Count} headers");
            _logger.LogInformation($"Row {rowNumber}: Headers found: [{string.Join(", ", headers.Select((h, i) => $"{i}: '{h}'"))}]");

            for (int i = 0; i < Math.Min(row.Count, headers.Count); i++)
            {
                var header = headers[i].ToLower().Trim();
                var value = i < row.Count ? row[i]?.Trim() ?? "" : "";

                // Map common column names to our fields
                switch (header)
                {
                    case "name":
                    case "company name":
                    case "business name":
                    case "company":
                    case "customer":  // Added for your sheet
                        company.Name = value;
                        _logger.LogDebug($"Row {rowNumber}: Found name '{value}' in column '{headers[i]}'");
                        break;
                    case "place id":
                    case "google place id":
                    case "placeid":
                        company.PlaceId = value;
                        break;
                    case "google maps url":
                    case "maps url":
                    case "url":
                    case "google url":
                    case "google id":  // Added for your sheet
                        company.GoogleMapsUrl = value;
                        break;
                    case "address":
                    case "location":
                        company.Address = value;
                        break;
                    case "phone":
                    case "phone number":
                    case "tel":
                        company.Phone = value;
                        break;
                    case "website":
                    case "web":
                    case "site":
                        company.Website = value;
                        break;
                    case "email":
                    case "email address":
                    case "e-mail":
                    case "mail":
                    case "contact email":
                        company.EmailAddress = value;
                        _logger.LogDebug($"Row {rowNumber}: Found email '{value}' in column '{headers[i]}'");
                        break;
                    case "category":
                    case "type":
                    case "business type":
                        company.Category = value;
                        break;
                    default:
                        // Store any additional data
                        if (!string.IsNullOrEmpty(value))
                        {
                            company.AdditionalData[headers[i]] = value;
                        }
                        break;
                }
            }

            // If we have a Google Maps URL but no Place ID, try to extract it
            if (!string.IsNullOrEmpty(company.GoogleMapsUrl) && string.IsNullOrEmpty(company.PlaceId))
            {
                company.PlaceId = ExtractPlaceIdFromGoogleMapsUrl(company.GoogleMapsUrl);
                if (!string.IsNullOrEmpty(company.PlaceId))
                {
                    _logger.LogInformation($"Row {rowNumber}: Extracted Place ID '{company.PlaceId}' from Google Maps URL");
                }
            }

            _logger.LogDebug($"Row {rowNumber} parsed: Name='{company.Name}', PlaceId='{company.PlaceId}', EmailAddress='{company.EmailAddress}', HasAdditionalData={company.AdditionalData.Count}");
            _logger.LogInformation($"Row {rowNumber}: Parsed company '{company.Name}' with email '{company.EmailAddress ?? "NULL"}'");
            return company;
        }

        private static string? ExtractPlaceIdFromGoogleMapsUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            try
            {
                // Modern comprehensive Google Maps URL Place ID extraction
                // Handles various URL formats from 2020-2024+

                // 1. Direct place_id parameter (most reliable when present)
                var match = Regex.Match(url, @"place_id=([a-zA-Z0-9\-_]{20,})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 2. ChIJ Place ID anywhere in URL (most common format)
                match = Regex.Match(url, @"(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 3. Modern !1s format (2023+ URLs)
                match = Regex.Match(url, @"!1s(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 4. !4m format (common in shared URLs)
                match = Regex.Match(url, @"!4m.*?!1s(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 5. !3m format (another variant)
                match = Regex.Match(url, @"!3m.*?!1s(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 6. Data parameter extraction (encoded URLs)
                match = Regex.Match(url, @"data=.*?(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 7. URL path format /place/ChIJ...
                match = Regex.Match(url, @"/place/(ChIJ[a-zA-Z0-9\-_]{16,35})");
                if (match.Success && IsValidPlaceId(match.Groups[1].Value))
                {
                    return match.Groups[1].Value;
                }

                // 8. Try URL decoding first for encoded URLs
                try
                {
                    var decodedUrl = System.Web.HttpUtility.UrlDecode(url);
                    if (decodedUrl != url) // Only recurse if URL was actually encoded
                    {
                        var decodedResult = ExtractPlaceIdFromGoogleMapsUrl(decodedUrl);
                        if (!string.IsNullOrEmpty(decodedResult))
                            return decodedResult;
                    }
                }
                catch
                {
                    // URL decoding failed, continue
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool IsValidPlaceId(string placeId)
        {
            if (string.IsNullOrEmpty(placeId))
                return false;

            // Valid Place IDs typically:
            // - Start with ChIJ
            // - Are around 27 characters long
            // - Contain base64-like characters
            return placeId.StartsWith("ChIJ") && 
                   placeId.Length >= 20 && 
                   placeId.Length <= 35 &&
                   !placeId.StartsWith("0x"); // Exclude hex values
        }

        private async Task<GoogleCredential?> CreateGoogleCredentialAsync(string credentialSource, bool isFilePath, string? scopes = null)
        {
            var maxRetries = 3;
            var currentRetry = 0;

            while (currentRetry < maxRetries)
            {
                try
                {
                    GoogleCredential credential;

                    if (isFilePath)
                    {
                        // Load from file
                        using var stream = new FileStream(credentialSource, FileMode.Open, FileAccess.Read);
                        credential = GoogleCredential.FromStream(stream);
                    }
                    else
                    {
                        // Load from JSON string
                        var keyBytes = Encoding.UTF8.GetBytes(credentialSource);
                        using var stream = new MemoryStream(keyBytes);
                        credential = GoogleCredential.FromStream(stream);
                    }

                    // Create scoped credential
                    var scopedCredential = credential.CreateScoped(scopes ?? DriveService.Scope.DriveReadonly);
                    
                    // Test the credential by trying to get an access token
                    var tokenRequest = scopedCredential.UnderlyingCredential as Google.Apis.Auth.OAuth2.ServiceAccountCredential;
                    if (tokenRequest != null)
                    {
                        _logger.LogInformation("Successfully created Google service account credential");
                        var token = await tokenRequest.GetAccessTokenForRequestAsync();
                        if (!string.IsNullOrEmpty(token))
                        {
                            _logger.LogInformation("Successfully obtained access token from Google");
                            return scopedCredential;
                        }
                    }

                    return scopedCredential;
                }
                catch (System.Security.Cryptography.CryptographicException cryptoEx)
                {
                    _logger.LogWarning($"Cryptographic error attempt {currentRetry + 1}/{maxRetries}: {cryptoEx.Message}");
                    
                    if (currentRetry == maxRetries - 1)
                    {
                        _logger.LogError(cryptoEx, "Failed to create Google credential after all retry attempts due to cryptographic error");
                        
                        // Try alternative authentication method as last resort
                        return await TryAlternativeAuthenticationAsync(credentialSource, isFilePath, scopes);
                    }
                    
                    // Wait before retry
                    await Task.Delay(1000 * (currentRetry + 1));
                    currentRetry++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error creating Google credential: {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        private async Task<GoogleCredential?> TryAlternativeAuthenticationAsync(string credentialSource, bool isFilePath, string scopes)
        {
            try
            {
                _logger.LogInformation("Attempting alternative authentication method using environment variable approach");
                
                string tempKeyFilePath = null;
                
                try
                {
                    if (!isFilePath)
                    {
                        // Create a temporary file for the service account key
                        tempKeyFilePath = Path.GetTempFileName();
                        await System.IO.File.WriteAllTextAsync(tempKeyFilePath, credentialSource);
                    }
                    else
                    {
                        tempKeyFilePath = credentialSource;
                    }

                    // Set the environment variable that Google libraries look for
                    var originalEnvVar = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", tempKeyFilePath);

                    try
                    {
                        // Try using default credentials which should pick up the environment variable
                        var credential = GoogleCredential.GetApplicationDefault()
                            .CreateScoped(scopes ?? DriveService.Scope.DriveReadonly);

                        _logger.LogInformation("Successfully created Google credential using alternative method");
                        return credential;
                    }
                    finally
                    {
                        // Restore original environment variable
                        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", originalEnvVar);
                    }
                }
                finally
                {
                    // Clean up temp file if we created one
                    if (!isFilePath && tempKeyFilePath != null && System.IO.File.Exists(tempKeyFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(tempKeyFilePath);
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogWarning(deleteEx, "Failed to delete temporary credentials file");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alternative authentication method also failed");
                return null;
            }
        }

        public void Dispose()
        {
            _driveService?.Dispose();
            _sheetsService?.Dispose();
        }
    }
}