using System.ComponentModel.DataAnnotations;

namespace google_reviews.Models
{
    public class Review
    {
        public string Id { get; set; } = "";
        
        [Required]
        public string CompanyId { get; set; } = "";
        
        [Display(Name = "Author Name")]
        public string AuthorName { get; set; } = "";
        
        [Range(1, 5)]
        public int Rating { get; set; }
        
        [Display(Name = "Review Text")]
        public string? Text { get; set; }
        
        [Display(Name = "Review Date")]
        public DateTime Time { get; set; }
        
        [Display(Name = "Author Profile URL")]
        public string? AuthorUrl { get; set; }
        
        [Display(Name = "Author Photo URL")]
        public string? ProfilePhotoUrl { get; set; }
        
        // Navigation property
        public virtual Company? Company { get; set; }
    }
}