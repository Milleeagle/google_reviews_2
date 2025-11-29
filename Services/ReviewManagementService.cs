using google_reviews.Data;
using Microsoft.EntityFrameworkCore;

namespace google_reviews.Services
{
    public class ReviewManagementService : IReviewManagementService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewManagementService> _logger;

        public ReviewManagementService(ApplicationDbContext context, ILogger<ReviewManagementService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> DeleteAllReviewsAsync()
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var count = await _context.Reviews.CountAsync();
                _context.Reviews.RemoveRange(_context.Reviews);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted all {Count} reviews", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all reviews");
                throw;
            }
        }

        public async Task<int> DeleteReviewsByInitialAsync(char initial)
        {
            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                var initialUpper = char.ToUpper(initial);
                var reviewsToDelete = await _context.Reviews
                    .Include(r => r.Company)
                    .Where(r => r.Company != null && r.Company.Name.ToUpper().StartsWith(initialUpper.ToString()))
                    .ToListAsync();

                var count = reviewsToDelete.Count;
                _context.Reviews.RemoveRange(reviewsToDelete);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Deleted {Count} reviews for companies starting with '{Initial}'", count, initialUpper);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting reviews by initial '{Initial}'", initial);
                throw;
            }
        }

        public async Task<(int totalReviews, int totalCompanies)> GetReviewStatsAsync()
        {
            try
            {
                var totalReviews = await _context.Reviews.CountAsync();
                var totalCompanies = await _context.Companies.CountAsync();

                return (totalReviews, totalCompanies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting review stats");
                throw;
            }
        }
    }
}
