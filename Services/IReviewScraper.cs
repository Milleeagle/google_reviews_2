using google_reviews.Models;

namespace google_reviews.Services;

public interface IReviewScraper
{
    Task<List<Review>> ScrapeReviewsAsync(string placeId);
    void Dispose();
}
