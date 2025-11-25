using ClosedXML.Excel;
using google_reviews.Models;

namespace google_reviews.Services
{
    public class ExcelService : IExcelService
    {
        private readonly ILogger<ExcelService> _logger;

        public ExcelService(ILogger<ExcelService> logger)
        {
            _logger = logger;
        }

        public byte[] GenerateReviewReportExcel(ReviewMonitorReport report)
        {
            try
            {
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Review Report");

                // Set up headers (Swedish/English mix as requested)
                worksheet.Cell(1, 1).Value = "Företagnamn"; // Company Name
                worksheet.Cell(1, 2).Value = "Mailadress"; // Email Address
                worksheet.Cell(1, 3).Value = "Rating";
                worksheet.Cell(1, 4).Value = "Review";
                worksheet.Cell(1, 5).Value = "Review Link";
                worksheet.Cell(1, 6).Value = "Review Date";
                worksheet.Cell(1, 7).Value = "Author";

                // Style the header row
                var headerRange = worksheet.Range(1, 1, 1, 7);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                int currentRow = 2;

                _logger.LogInformation($"Excel generation: Processing {report.CompanyReports.Count} company reports");

                // Add data for each company and their reviews
                foreach (var companyReport in report.CompanyReports)
                {
                    var company = companyReport.Company;

                    foreach (var review in companyReport.BadReviews)
                    {
                        worksheet.Cell(currentRow, 1).Value = company.Name;
                        var emailValue = company.EmailAddress ?? "";
                        worksheet.Cell(currentRow, 2).Value = emailValue;

                        // Debug logging
                        _logger.LogInformation($"Company: {company.Name}, EmailAddress property: '{company.EmailAddress}', Using value: '{emailValue}'");
                        worksheet.Cell(currentRow, 3).Value = review.Rating;
                        worksheet.Cell(currentRow, 4).Value = review.Text;

                        // Generate link to company's Google Reviews page
                        var reviewLink = "";

                        // Priority 1: Use Place ID to create direct link to reviews
                        if (!string.IsNullOrEmpty(company.PlaceId))
                        {
                            // Direct link to reviews tab for the place
                            reviewLink = $"https://www.google.com/maps/place/?q=place_id:{company.PlaceId}&hl=en#lkt=reviews";
                        }
                        // Priority 2: Use Google Maps URL if Place ID is not available
                        else if (!string.IsNullOrEmpty(company.GoogleMapsUrl))
                        {
                            reviewLink = company.GoogleMapsUrl;
                        }
                        // Priority 3: Create search link using company name as fallback
                        else if (!string.IsNullOrEmpty(company.Name))
                        {
                            var encodedName = Uri.EscapeDataString(company.Name);
                            reviewLink = $"https://www.google.com/maps/search/{encodedName}";
                        }

                        worksheet.Cell(currentRow, 5).Value = reviewLink;
                        worksheet.Cell(currentRow, 6).Value = review.Time.ToString("yyyy-MM-dd");
                        worksheet.Cell(currentRow, 7).Value = review.AuthorName;

                        currentRow++;
                    }
                }

                // If no data was added, add a "No data" row to make sure the Excel file is valid
                if (currentRow == 2)
                {
                    worksheet.Cell(2, 1).Value = "No review data found for the specified criteria";
                    worksheet.Cell(2, 2).Value = "";
                    worksheet.Cell(2, 3).Value = "";
                    worksheet.Cell(2, 4).Value = "";
                    worksheet.Cell(2, 5).Value = "";
                    worksheet.Cell(2, 6).Value = "";
                    worksheet.Cell(2, 7).Value = "";
                    currentRow = 3;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                // Set column widths for better readability
                worksheet.Column(1).Width = 25; // Företagnamn
                worksheet.Column(2).Width = 30; // Mailadress
                worksheet.Column(3).Width = 10; // Rating
                worksheet.Column(4).Width = 50; // Review
                worksheet.Column(5).Width = 40; // Review Link
                worksheet.Column(6).Width = 15; // Review Date
                worksheet.Column(7).Width = 20; // Author

                // Wrap text in review column
                worksheet.Column(4).Style.Alignment.WrapText = true;

                // Add borders to all data
                if (currentRow > 2)
                {
                    var dataRange = worksheet.Range(2, 1, currentRow - 1, 7);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                }

                // Save to memory stream and return as byte array
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                _logger.LogInformation($"Generated Excel report with {currentRow - 2} review entries");
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Excel report");
                throw;
            }
        }

        public List<Company> ImportCompaniesFromExcel(Stream fileStream)
        {
            var companies = new List<Company>();

            try
            {
                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheets.First();

                // Start from row 2 (skip header row)
                var rows = worksheet.RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    var companyName = row.Cell(1).GetValue<string>()?.Trim();
                    var emailAddress = row.Cell(2).GetValue<string>()?.Trim();
                    var placeId = row.Cell(3).GetValue<string>()?.Trim();

                    // Skip empty rows
                    if (string.IsNullOrWhiteSpace(companyName))
                        continue;

                    var company = new Company
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = companyName,
                        EmailAddress = string.IsNullOrWhiteSpace(emailAddress) ? null : emailAddress,
                        PlaceId = string.IsNullOrWhiteSpace(placeId) ? null : placeId,
                        IsActive = true,
                        IsCurrentCustomer = true,
                        LastUpdated = DateTime.UtcNow
                    };

                    companies.Add(company);
                    _logger.LogInformation($"Parsed company from Excel: {companyName} (PlaceId: {placeId ?? "none"})");
                }

                _logger.LogInformation($"Successfully parsed {companies.Count} companies from Excel file");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing companies from Excel");
                throw new InvalidOperationException("Failed to parse Excel file. Please ensure the file format is correct (Column 1: Company Name, Column 2: Email Address, Column 3: Place ID)", ex);
            }

            return companies;
        }
    }
}