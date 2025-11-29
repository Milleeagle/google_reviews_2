using google_reviews.Models;

namespace google_reviews.Services;

public interface IReviewScraper
{
    Task<List<Review>> ScrapeReviewsAsync(string googleMapsUrl, ScrapingOptions? options = null);
    Task<Company?> SearchCompanyAsync(string companyName, string? location = null);
    Task<Company?> ScrapeCompanyWithReviewsAsync(string companyName, string? location = null, ScrapingOptions? options = null);
    Task<Company?> ExtractCompanyInfoAsync(string googleMapsUrl);
    Task<Company?> ScrapeReviewsByPlaceIdAsync(string placeId, ScrapingOptions? options = null);
    void Dispose();
}
