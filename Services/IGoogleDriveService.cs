using google_reviews.Models;

namespace google_reviews.Services
{
    public interface IGoogleDriveService
    {
        Task<GoogleDriveDocument?> GetDocumentAsync(string documentId);
        Task<GoogleDriveDocument?> GetDocumentByUrlAsync(string shareUrl);
        Task<List<GoogleDriveDocument>> ListSharedDocumentsAsync();
        Task<bool> TestDriveConnectionAsync();
        Task<GoogleSheetData?> GetSheetDataAsync(string documentId, string? sheetName = null);
        Task<List<SheetCompany>> ParseCompaniesFromSheetAsync(string documentId, string? sheetName = null);
    }
}