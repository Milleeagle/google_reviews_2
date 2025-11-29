namespace google_reviews.Models
{
    public class CustomerEmailData
    {
        public string CompanyName { get; set; } = "";
        public string ContactName { get; set; } = ""; // If provided, use this instead of company name in greeting
        public string Email { get; set; } = "";
        public string GoogleMapsUrl { get; set; } = "";
        public int BadReviewCount { get; set; }
        public double AverageRating { get; set; }
        public List<Review> BadReviews { get; set; } = new List<Review>();

        // Language detection (automatically set based on content)
        public Language Language { get; set; } = Language.Swedish;

        // Company template selection (set by user via dropdown)
        public CompanyTemplate CompanyTemplate { get; set; } = CompanyTemplate.Deletify;

        // DEPRECATED: Use Language enum instead
        [Obsolete("Use Language property instead")]
        public bool IsSwedish { get; set; } = true;

        // Helper property to get the name to use in greeting
        public string GreetingName => !string.IsNullOrWhiteSpace(ContactName) ? ContactName : CompanyName;
    }

    public class BatchEmailResult
    {
        public int TotalEmails { get; set; }
        public int SuccessCount { get; set; }
        public int FailCount { get; set; }
        public List<string> FailedEmails { get; set; } = new List<string>();
        public List<string> SuccessfulEmails { get; set; } = new List<string>();
        public bool IsTestMode { get; set; }
        public string? TestEmail { get; set; }
    }
}
