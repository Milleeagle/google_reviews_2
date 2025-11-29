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

        [Display(Name = "Email Address")]
        [EmailAddress]
        public string? EmailAddress { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Current Customer")]
        public bool IsCurrentCustomer { get; set; } = false;

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        [Display(Name = "Address")]
        public string? Address { get; set; }

        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Website")]
        [Url]
        public string? Website { get; set; }

        [Display(Name = "Overall Rating")]
        [Range(0.0, 5.0)]
        public double? OverallRating { get; set; }

        // Navigation property
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}