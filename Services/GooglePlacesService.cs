using google_reviews.Models;
using System.Text.Json;
using System.Threading;

namespace google_reviews.Services
{
    public class GooglePlacesService : IGooglePlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GooglePlacesService> _logger;
        private readonly string? _apiKey;

        // Rate limiting: Updated to 5000 requests per minute as per user configuration
        private static readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(50, 50); // Allow concurrent requests
        private static readonly Queue<DateTime> _requestTimes = new Queue<DateTime>();
        private static readonly object _requestTimesLock = new object();
        private static readonly TimeSpan _rateLimitWindow = TimeSpan.FromMinutes(1);
        private static readonly int _maxRequestsPerMinute = 5000;

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

        private async Task<HttpResponseMessage> MakeRateLimitedRequestAsync(Func<Task<HttpResponseMessage>> request, int maxRetries = 3)
        {
            await _rateLimitSemaphore.WaitAsync();
            try
            {
                // Sliding window rate limiting for 5000 requests per minute
                DateTime now = DateTime.UtcNow;
                TimeSpan waitTime = TimeSpan.Zero;

                lock (_requestTimesLock)
                {
                    // Remove requests older than 1 minute
                    while (_requestTimes.Count > 0 && (now - _requestTimes.Peek()) > _rateLimitWindow)
                    {
                        _requestTimes.Dequeue();
                    }

                    // Check if we're at the rate limit
                    if (_requestTimes.Count >= _maxRequestsPerMinute)
                    {
                        var oldestRequest = _requestTimes.Peek();
                        waitTime = _rateLimitWindow - (now - oldestRequest);
                    }
                }

                // Wait outside the lock if needed
                if (waitTime > TimeSpan.Zero)
                {
                    _logger.LogInformation($"Rate limiting: Waiting {waitTime.TotalMilliseconds}ms (5000 requests/minute limit)");
                    await Task.Delay(waitTime);

                    // Clean up after waiting
                    lock (_requestTimesLock)
                    {
                        now = DateTime.UtcNow;
                        while (_requestTimes.Count > 0 && (now - _requestTimes.Peek()) > _rateLimitWindow)
                        {
                            _requestTimes.Dequeue();
                        }
                    }
                }

                int attempt = 0;
                while (attempt <= maxRetries)
                {
                    try
                    {
                        // Record this request time
                        lock (_requestTimesLock)
                        {
                            _requestTimes.Enqueue(DateTime.UtcNow);
                        }

                        var response = await request();

                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            attempt++;
                            if (attempt <= maxRetries)
                            {
                                var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                                _logger.LogWarning($"Rate limit exceeded (429). Retry attempt {attempt}/{maxRetries} in {retryDelay.TotalSeconds} seconds");
                                await Task.Delay(retryDelay);
                                continue;
                            }
                        }

                        return response;
                    }
                    catch (Exception ex) when (attempt < maxRetries)
                    {
                        attempt++;
                        var retryDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        _logger.LogWarning(ex, $"Request failed. Retry attempt {attempt}/{maxRetries} in {retryDelay.TotalSeconds} seconds");
                        await Task.Delay(retryDelay);
                    }
                }

                throw new InvalidOperationException($"Request failed after {maxRetries} retries");
            }
            finally
            {
                _rateLimitSemaphore.Release();
            }
        }

        public async Task<CompanyReviewData?> GetReviewsAsync(Company company)
        {
            return await GetReviewsAsync(company, true); // Default to throttling enabled
        }

        public async Task<CompanyReviewData?> GetReviewsAsync(Company company, bool enableThrottling)
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

                _logger.LogInformation($"Fetching reviews for {company.Name} (Place ID: {company.PlaceId}) using New Places API (Throttling: {enableThrottling})");

                HttpResponseMessage response;
                if (enableThrottling)
                {
                    response = await MakeRateLimitedRequestAsync(() => _httpClient.GetAsync(url));
                }
                else
                {
                    response = await _httpClient.GetAsync(url);
                }
                
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

                var response = await MakeRateLimitedRequestAsync(() => _httpClient.GetAsync(url));
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

        public async Task<string?> SearchPlaceByNameAsync(string businessName, string? address = null)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogError("Google Places API key not configured");
                return null;
            }

            if (string.IsNullOrEmpty(businessName))
            {
                _logger.LogWarning("Business name is required for search");
                return null;
            }

            try
            {
                // Create search query
                var searchQuery = businessName;
                if (!string.IsNullOrEmpty(address))
                {
                    searchQuery += $", {address}";
                }

                // Using the Google Places API (New) Text Search endpoint
                var url = "https://places.googleapis.com/v1/places:searchText";

                var requestBody = new
                {
                    textQuery = searchQuery,
                    maxResultCount = 5 // Limit results to avoid unnecessary data
                };

                var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation($"Searching for place: '{searchQuery}' using Google Places Search API");

                var response = await MakeRateLimitedRequestAsync(() => {
                    var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = content
                    };
                    // Add required headers for the new API without affecting the shared HttpClient
                    request.Headers.Add("X-Goog-Api-Key", _apiKey);
                    request.Headers.Add("X-Goog-FieldMask", "places.id,places.displayName,places.formattedAddress");

                    return _httpClient.SendAsync(request);
                });
                var jsonResponse = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Search API Response Status: {response.StatusCode}");
                _logger.LogInformation($"Search API Response: {jsonResponse}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Search API request failed with status {response.StatusCode}: {jsonResponse}");
                    return null;
                }

                var searchResponse = JsonSerializer.Deserialize<GooglePlacesSearchResponse>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                if (searchResponse?.Places?.Any() == true)
                {
                    var firstPlace = searchResponse.Places.First();
                    var displayName = firstPlace.DisplayName?.Text ?? "Unknown";
                    _logger.LogInformation($"Found Place ID: {firstPlace.Id} for '{businessName}' - {displayName} at {firstPlace.FormattedAddress}");
                    return firstPlace.Id;
                }

                _logger.LogWarning($"No places found for search query: '{searchQuery}'");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error searching for place: '{businessName}'");
                return null;
            }
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