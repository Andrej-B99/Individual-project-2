using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MasterServicePlatform.Web.Models
{
    public enum VerificationStatus
    {
        Unverified,
        Pending,
        Verified,
        Rejected
    }

    public class Master
    {
        public int Id { get; set; }

        public ApplicationUser? User { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [Required]
        [Display(Name = "City")]
        public string City { get; set; } = "";

        [Required]
        public string Profession { get; set; } = "";

        [Range(0, 50)]
        [Display(Name = "Experience (years)")]
        public int ExperienceYears { get; set; }

        [Range(1, 10000)]
        [Display(Name = "Price per hour")]
        public decimal PricePerHour { get; set; }

        [Required, Phone]
        public string Phone { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        public string? AvatarPath { get; set; }

        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Unverified;

        public List<Review> Reviews { get; set; } = new();

        public List<PortfolioPhoto> PortfolioPhotos { get; set; } = new();

        [NotMapped]
        public double AverageRating =>
            Reviews == null || Reviews.Count == 0 ? 0 : Reviews.Average(r => r.Rating);

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";
    }
}
