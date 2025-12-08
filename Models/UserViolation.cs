using System;

namespace MasterServicePlatform.Web.Models
{
    public class UserViolation
    {
        public int Id { get; set; }

        // Who violates
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // When happend
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Type
        public ViolationType Type { get; set; }

        // Extra info
        public string? Details { get; set; }

        // If connected to order or review
        public int? RelatedOrderId { get; set; }
        public int? RelatedReviewId { get; set; }
    }
}
