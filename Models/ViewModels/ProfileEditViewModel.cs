namespace MasterServicePlatform.Web.Models.ViewModels
{
    public class ProfileEditViewModel
    {
        public string UserId { get; set; }

        // данные из ApplicationUser
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Phone { get; set; }

        // данные из UserProfile
        public string City { get; set; }
    }
}
