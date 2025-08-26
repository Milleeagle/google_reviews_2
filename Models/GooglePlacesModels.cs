namespace google_reviews.Models
{
    // Legacy API response models (keeping for reference)
    public class GooglePlacesResponse
    {
        public GooglePlaceResult? Result { get; set; }
        public string Status { get; set; } = "";
    }

    public class GooglePlaceResult
    {
        public List<GooglePlaceReview> Reviews { get; set; } = new();
        public double Rating { get; set; }
        public int UserRatingsTotal { get; set; }
    }

    public class GooglePlaceReview
    {
        public string AuthorName { get; set; } = "";
        public string AuthorUrl { get; set; } = "";
        public string ProfilePhotoUrl { get; set; } = "";
        public int Rating { get; set; }
        public string RelativeTimeDescription { get; set; } = "";
        public string Text { get; set; } = "";
        public long Time { get; set; }
    }

    // New Places API response models
    public class NewGooglePlacesResponse
    {
        public List<NewGooglePlaceReview>? Reviews { get; set; }
        public double Rating { get; set; }
        public int UserRatingCount { get; set; }
    }

    public class NewGooglePlaceReview
    {
        public string? Name { get; set; }
        public string? RelativePublishTimeDescription { get; set; }
        public int Rating { get; set; }
        public NewGoogleReviewText? Text { get; set; }
        public NewGoogleReviewAuthor? AuthorAttribution { get; set; }
        public string? PublishTime { get; set; }
    }

    public class NewGoogleReviewText
    {
        public string? Text { get; set; }
        public string? LanguageCode { get; set; }
    }

    public class NewGoogleReviewAuthor
    {
        public string? DisplayName { get; set; }
        public string? Uri { get; set; }
        public string? PhotoUri { get; set; }
    }

    // Service response model
    public class CompanyReviewData
    {
        public string CompanyId { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<Review> Reviews { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}