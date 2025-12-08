using MasterServicePlatform.Web.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MasterServicePlatform.Web.Models
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Master> Masters { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Order> Orders { get; set; }

        public DbSet<PortfolioPhoto> PortfolioPhotos { get; set; }

        public DbSet<UserViolation> UserViolations { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Price precision
            builder.Entity<Master>()
                .Property(m => m.PricePerHour)
                .HasPrecision(18, 2);

            // ApplicationUser <-> UserProfile (1:1)
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserProfile>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // ApplicationUser <-> Master (1:1)
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Master)
                .WithOne()
                .HasForeignKey<ApplicationUser>(u => u.MasterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .Property(o => o.Budget)
                .HasPrecision(18, 2);

        }
    }
}
