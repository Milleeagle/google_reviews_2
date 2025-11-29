namespace google_reviews.Models
{
    public class ScrapingOptions
    {
        public int MaxReviews { get; set; } = 100;
        public DateTime? FromDate { get; set; }
        public SortOrder SortBy { get; set; } = SortOrder.MostRelevant;
    }

    public enum SortOrder
    {
        MostRelevant,
        Newest,
        HighestRating,
        LowestRating
    }
}
