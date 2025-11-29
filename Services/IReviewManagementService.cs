namespace google_reviews.Services
{
    public interface IReviewManagementService
    {
        Task<int> DeleteAllReviewsAsync();
        Task<int> DeleteReviewsByInitialAsync(char initial);
        Task<(int totalReviews, int totalCompanies)> GetReviewStatsAsync();
    }
}
