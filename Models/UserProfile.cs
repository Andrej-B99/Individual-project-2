using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterServicePlatform.Web.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        // Name / Surname
        [Required]
        public string FirstName { get; set; } = "";

        [Required]
        public string LastName { get; set; } = "";

        // Extra data
        public string? Phone { get; set; }
        public string? City { get; set; }

        // FK to Identity User
        [Required]
        public string UserId { get; set; } = "";

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
