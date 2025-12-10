using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterServicePlatform.Web.Models
{
    public class Message
    {
        public int Id { get; set; }

        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string Text { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;


        [ForeignKey(nameof(SenderId))]
        public ApplicationUser Sender { get; set; }

        [ForeignKey(nameof(ReceiverId))]
        public ApplicationUser Receiver { get; set; }
        public DateTime? EditedAt { get; set; }


    }
}
