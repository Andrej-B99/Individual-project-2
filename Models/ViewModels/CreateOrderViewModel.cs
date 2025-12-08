using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MasterServicePlatform.Web.Models.ViewModels

{
    public class CreateOrderViewModel
    {
        public int MasterId { get; set; }

        [Required, StringLength(1000)]
        public string Description { get; set; }

        [Required, StringLength(300)]
        public string Address { get; set; }

        public decimal? Budget { get; set; }

        public DateTime? PreferredDate { get; set; }

        public TimeSpan? PreferredTime { get; set; }

        [Required, StringLength(100)]
        public string ServiceCategory { get; set; }

        public IFormFile Attachment { get; set; } // файл
    }
}
