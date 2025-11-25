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

    // Google Drive models
    public class GoogleDriveDocument
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string MimeType { get; set; } = "";
        public string? Description { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ModifiedTime { get; set; }
        public string? WebViewLink { get; set; }
        public string? WebContentLink { get; set; }
        public long Size { get; set; }
        public List<string> Owners { get; set; } = new();
        public bool Shared { get; set; }
        public string? Content { get; set; } // For text documents
    }

    // Google Sheets models
    public class GoogleSheetData
    {
        public string DocumentId { get; set; } = "";
        public string SheetName { get; set; } = "";
        public List<string> Headers { get; set; } = new();
        public List<List<string>> Rows { get; set; } = new();
        public int TotalRows { get; set; }
        public int TotalColumns { get; set; }
    }

    public class SheetCompany
    {
        public int RowNumber { get; set; }
        public string Name { get; set; } = "";
        public string? PlaceId { get; set; }
        public string? GoogleMapsUrl { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Website { get; set; }
        public string? EmailAddress { get; set; }
        public string? Category { get; set; }
        public Dictionary<string, string> AdditionalData { get; set; } = new();
        public bool AlreadyExists { get; set; }
        public string? ExistingCompanyId { get; set; }
    }

    // Google Places Search API models
    public class GooglePlacesSearchResponse
    {
        public List<GooglePlaceSearchResult> Places { get; set; } = new();
    }

    public class GooglePlaceSearchResult
    {
        public string? Id { get; set; }
        public GooglePlaceDisplayName? DisplayName { get; set; }
        public string? FormattedAddress { get; set; }
        public GooglePlaceLocation? Location { get; set; }
    }

    public class GooglePlaceDisplayName
    {
        public string? Text { get; set; }
        public string? LanguageCode { get; set; }
    }

    public class GooglePlaceLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    // Review monitoring models
    public class ReviewMonitorReport
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int MaxRating { get; set; }
        public List<CompanyReviewReport> CompanyReports { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public int TotalCompaniesChecked { get; set; }
        public int CompaniesWithIssues { get; set; }
        public int TotalBadReviews { get; set; }
    }

    public class CompanyReviewReport
    {
        public Company Company { get; set; } = new();
        public List<Review> BadReviews { get; set; } = new();
        public double AverageRating { get; set; }
        public int TotalBadReviews { get; set; }
    }
}