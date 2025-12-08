using System;
using System.ComponentModel.DataAnnotations;

namespace MasterServicePlatform.Web.Models
{
    public enum OrderStatus
    {
        Pending,
        Accepted,
        Rejected,
        Completed
    }

    public class Order
    {
        public int Id { get; set; }

        // Author of order
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        // Which master
        public int MasterId { get; set; }
        public Master Master { get; set; }

        // Category
        [Required]
        [StringLength(100)]
        public string ServiceCategory { get; set; }

        // Description
        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        // Adress
        [Required]
        [StringLength(300)]
        public string Address { get; set; }

        // Date
        [DataType(DataType.Date)]
        public DateTime? PreferredDate { get; set; }

        // Time
        [DataType(DataType.Time)]
        public TimeSpan? PreferredTime { get; set; }

        // Budget
        [Range(0, 100000)]
        public decimal? Budget { get; set; }

        // Files
        public string AttachmentPath { get; set; }

        // Status
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        // Data of creation
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
