using MasterServicePlatform.Web.Models;

namespace MasterServicePlatform.Web.Models.ViewModels
{
    public class AdminMasterDetailsViewModel
    {
        public Master Master { get; set; }
        public ApplicationUser User { get; set; }

        public List<Review> Reviews { get; set; }
        public List<Order> Orders { get; set; }

        public double AverageRating { get; set; }
    }
}
