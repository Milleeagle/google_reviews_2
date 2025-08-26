using System.ComponentModel.DataAnnotations;

namespace google_reviews.Models
{
    public class Company
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [Display(Name = "Company Name")]
        public string Name { get; set; } = "";
        
        [Display(Name = "Google Place ID")]
        public string? PlaceId { get; set; }
        
        [Display(Name = "Google Maps URL")]
        public string? GoogleMapsUrl { get; set; }
        
        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        
        // Navigation property
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}