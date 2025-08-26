using google_reviews.Models;

namespace google_reviews.Services
{
    public interface IGooglePlacesService
    {
        Task<CompanyReviewData?> GetReviewsAsync(Company company);
        Task<CompanyReviewData?> GetFilteredReviewsAsync(Company company, DateTime? fromDate = null, DateTime? toDate = null, int? minRating = null, int? maxRating = null);
        Task<bool> TestApiConnectionAsync();
    }
}