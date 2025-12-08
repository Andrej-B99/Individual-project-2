namespace MasterServicePlatform.Web.Models.ViewModels
{
    public class AdminUserDetailsViewModel
    {
        public ApplicationUser User { get; set; }
        public List<UserViolation> Violations { get; set; }
        public List<Order> Orders { get; set; }

        public List<Review> Reviews { get; set; }

    }
}
