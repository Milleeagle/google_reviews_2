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
                var totalCount = await _context.Reviews.CountAsync();
                _logger.LogInformation("Starting deletion of {Count} reviews in batches", totalCount);

                int deletedCount = 0;
                int batchSize = 1000;
                int batchNumber = 0;

                while (true)
                {
                    batchNumber++;

                    // Get a batch of review IDs
                    var batchIds = await _context.Reviews
                        .Select(r => r.Id)
                        .Take(batchSize)
                        .ToListAsync();

                    if (!batchIds.Any())
                        break;

                    _logger.LogInformation("Deleting batch {BatchNumber}: {Count} reviews", batchNumber, batchIds.Count);

                    // Delete the batch in a separate transaction
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    var reviewsToDelete = await _context.Reviews
                        .Where(r => batchIds.Contains(r.Id))
                        .ToListAsync();

                    _context.Reviews.RemoveRange(reviewsToDelete);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    deletedCount += reviewsToDelete.Count;
                    _logger.LogInformation("Progress: {Deleted}/{Total} reviews deleted", deletedCount, totalCount);

                    // Small delay to allow transaction log to clear
                    await Task.Delay(100);
                }

                _logger.LogInformation("Completed deletion of {Count} reviews", deletedCount);
                return deletedCount;
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
                var initialUpper = char.ToUpper(initial);

                // Get total count first
                var totalCount = await _context.Reviews
                    .Include(r => r.Company)
                    .Where(r => r.Company != null && r.Company.Name.ToUpper().StartsWith(initialUpper.ToString()))
                    .CountAsync();

                _logger.LogInformation("Starting deletion of {Count} reviews for companies starting with '{Initial}' in batches", totalCount, initialUpper);

                int deletedCount = 0;
                int batchSize = 1000;
                int batchNumber = 0;

                while (true)
                {
                    batchNumber++;

                    // Get a batch of review IDs
                    var batchIds = await _context.Reviews
                        .Include(r => r.Company)
                        .Where(r => r.Company != null && r.Company.Name.ToUpper().StartsWith(initialUpper.ToString()))
                        .Select(r => r.Id)
                        .Take(batchSize)
                        .ToListAsync();

                    if (!batchIds.Any())
                        break;

                    _logger.LogInformation("Deleting batch {BatchNumber} for '{Initial}': {Count} reviews", batchNumber, initialUpper, batchIds.Count);

                    // Delete the batch in a separate transaction
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    var reviewsToDelete = await _context.Reviews
                        .Where(r => batchIds.Contains(r.Id))
                        .ToListAsync();

                    _context.Reviews.RemoveRange(reviewsToDelete);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    deletedCount += reviewsToDelete.Count;
                    _logger.LogInformation("Progress: {Deleted}/{Total} reviews deleted for '{Initial}'", deletedCount, totalCount, initialUpper);

                    // Small delay to allow transaction log to clear
                    await Task.Delay(100);
                }

                _logger.LogInformation("Completed deletion of {Count} reviews for companies starting with '{Initial}'", deletedCount, initialUpper);
                return deletedCount;
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
