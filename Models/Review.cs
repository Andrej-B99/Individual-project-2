using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterServicePlatform.Web.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int MasterId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Master? Master { get; set; }

        public string? UserId { get; set; }

        public bool IsUserDeleted { get; set; } = false;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        public DateTime? LastEditedAt { get; set; }
    }
}
