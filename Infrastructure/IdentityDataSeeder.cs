using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MasterServicePlatform.Web.Models;

namespace MasterServicePlatform.Web.Infrastructure
{
    public static class IdentityDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Create required roles
            string[] roles = { "Admin", "Master", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create initial Admin user
            const string adminEmail = "admin@local.test";
            const string adminPassword = "Admin!123";

            var admin = await userManager.FindByEmailAsync(adminEmail);

            // If admin user does not exist, create it
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Role = UserRole.Admin,
                    IsVerified = true
                };

                var result = await userManager.CreateAsync(admin, adminPassword);

                // Assign Admin role to the new user
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}
