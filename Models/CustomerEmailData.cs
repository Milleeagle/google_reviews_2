namespace google_reviews.Models
{
    public class CustomerEmailData
    {
        public string CompanyName { get; set; } = "";
        public string Email { get; set; } = "";
        public string GoogleMapsUrl { get; set; } = "";
        public int BadReviewCount { get; set; }
        public double AverageRating { get; set; }
        public List<Review> BadReviews { get; set; } = new List<Review>();
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
