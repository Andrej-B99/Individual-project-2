using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace MasterServicePlatform.Web.Models
{
    public enum UserRole
    {
        User = 0,
        Master = 1,
        Admin = 2
    }

    public class ApplicationUser : IdentityUser
    {
        public UserRole Role { get; set; } = UserRole.User;
        public bool IsVerified { get; set; } = false;

        // Name / Surname
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Linked Master (1:1)
        public int? MasterId { get; set; }

        [ForeignKey(nameof(MasterId))]
        public virtual Master? Master { get; set; }

        // Linked UserProfile (1:1)
        public virtual UserProfile? Profile { get; set; }

        // Avatar
        public string? AvatarPath { get; set; }

        // Blocked flag
        public bool IsBlocked { get; set; } = false;
    }
}
