using google_reviews.Models;
using System.Text.Json;

namespace google_reviews.Services
{
    public class GooglePlacesService : IGooglePlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GooglePlacesService> _logger;
        private readonly string? _apiKey;

        public GooglePlacesService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<GooglePlacesService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _apiKey = _configuration["GooglePlaces:ApiKey"];
        }

        public async Task<CompanyReviewData?> GetReviewsAsync(Company company)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("Google Places API key not configured");
                return null;
            }

            if (string.IsNullOrEmpty(company.PlaceId))
            {
                _logger.LogWarning($"Company {company.Name} does not have a Place ID");
                return null;
            }

            try
            {
                // Using the new Places API (New) endpoint
                var url = $"https://places.googleapis.com/v1/places/{company.PlaceId}?fields=reviews,rating,userRatingCount&key={_apiKey}";

                _logger.LogInformation($"Fetching reviews for {company.Name} (Place ID: {company.PlaceId}) using New Places API");

                var response = await _httpClient.GetAsync(url);
                
                var jsonContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"API Response: {jsonContent}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"API request failed with status {response.StatusCode}: {jsonContent}");
                    return null;
                }

                var placesResponse = JsonSerializer.Deserialize<NewGooglePlacesResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (placesResponse == null)
                {
                    _logger.LogWarning($"Failed to deserialize response for company {company.Name}");
                    return null;
                }

                var reviewData = new CompanyReviewData
                {
                    CompanyId = company.Id,
                    CompanyName = company.Name,
                    AverageRating = placesResponse.Rating,
                    TotalReviews = placesResponse.UserRatingCount,
                    Reviews = ConvertNewGoogleReviewsToReviews(placesResponse.Reviews ?? new List<NewGooglePlaceReview>(), company.Id),
                    LastUpdated = DateTime.UtcNow
                };

                _logger.LogInformation($"Successfully fetched {reviewData.Reviews.Count} reviews for {company.Name}");
                return reviewData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching reviews for company {company.Name}");
                return null;
            }
        }

        public async Task<bool> TestApiConnectionAsync()
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("Google Places API key is null or empty");
                return false;
            }

            try
            {
                // Test with a well-known place (Google HQ) using new API
                var testPlaceId = "ChIJj61dQgK6j4AR4GeTYWZsKWw";
                var url = $"https://places.googleapis.com/v1/places/{testPlaceId}?fields=displayName&key={_apiKey}";
                
                _logger.LogInformation($"Testing Google Places API (New) with URL: {url.Replace(_apiKey, "***API_KEY***")}");
                
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation($"API Response Status: {response.StatusCode}");
                _logger.LogInformation($"API Response Content: {content}");
                
                if (response.IsSuccessStatusCode)
                {
                    // For the new API, success status means it's working
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Google Places API connection");
                return false;
            }
        }

        private List<Review> ConvertGoogleReviewsToReviews(List<GooglePlaceReview> googleReviews, string companyId)
        {
            var reviews = new List<Review>();

            foreach (var googleReview in googleReviews)
            {
                var review = new Review
                {
                    Id = $"{companyId}_{googleReview.Time}",
                    CompanyId = companyId,
                    AuthorName = googleReview.AuthorName,
                    Rating = googleReview.Rating,
                    Text = googleReview.Text,
                    Time = DateTimeOffset.FromUnixTimeSeconds(googleReview.Time).DateTime,
                    AuthorUrl = googleReview.AuthorUrl,
                    ProfilePhotoUrl = googleReview.ProfilePhotoUrl
                };

                reviews.Add(review);
            }

            return reviews.OrderByDescending(r => r.Time).ToList();
        }

        private List<Review> ConvertNewGoogleReviewsToReviews(List<NewGooglePlaceReview> googleReviews, string companyId)
        {
            var reviews = new List<Review>();

            foreach (var googleReview in googleReviews)
            {
                DateTime reviewTime = DateTime.UtcNow; // Default fallback
                
                // Try to parse the publishTime if available
                if (!string.IsNullOrEmpty(googleReview.PublishTime))
                {
                    if (DateTime.TryParse(googleReview.PublishTime, out DateTime parsedTime))
                    {
                        reviewTime = parsedTime;
                    }
                }

                var review = new Review
                {
                    Id = $"{companyId}_{googleReview.Name ?? Guid.NewGuid().ToString()}",
                    CompanyId = companyId,
                    AuthorName = googleReview.AuthorAttribution?.DisplayName ?? "Anonymous",
                    Rating = googleReview.Rating,
                    Text = googleReview.Text?.Text ?? "",
                    Time = reviewTime,
                    AuthorUrl = googleReview.AuthorAttribution?.Uri ?? "",
                    ProfilePhotoUrl = googleReview.AuthorAttribution?.PhotoUri ?? ""
                };

                reviews.Add(review);
            }

            return reviews.OrderByDescending(r => r.Time).ToList();
        }

        public async Task<CompanyReviewData?> GetFilteredReviewsAsync(Company company, DateTime? fromDate = null, DateTime? toDate = null, int? minRating = null, int? maxRating = null)
        {
            // First, get all reviews
            var allReviewsData = await GetReviewsAsync(company);
            if (allReviewsData == null)
            {
                return null;
            }

            // Apply filters
            var filteredReviews = allReviewsData.Reviews.AsQueryable();

            if (fromDate.HasValue)
            {
                filteredReviews = filteredReviews.Where(r => r.Time >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                filteredReviews = filteredReviews.Where(r => r.Time <= toDate.Value);
            }

            if (minRating.HasValue)
            {
                filteredReviews = filteredReviews.Where(r => r.Rating >= minRating.Value);
            }

            if (maxRating.HasValue)
            {
                filteredReviews = filteredReviews.Where(r => r.Rating <= maxRating.Value);
            }

            var filteredList = filteredReviews.OrderByDescending(r => r.Time).ToList();

            // Recalculate averages based on filtered results
            var filteredData = new CompanyReviewData
            {
                CompanyId = allReviewsData.CompanyId,
                CompanyName = allReviewsData.CompanyName,
                AverageRating = filteredList.Any() ? filteredList.Average(r => r.Rating) : 0,
                TotalReviews = filteredList.Count,
                Reviews = filteredList,
                LastUpdated = allReviewsData.LastUpdated
            };

            _logger.LogInformation($"Filtered reviews for {company.Name}: {filteredList.Count} out of {allReviewsData.Reviews.Count} total reviews");
            return filteredData;
        }
    }
}