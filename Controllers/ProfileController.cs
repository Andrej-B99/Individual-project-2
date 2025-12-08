using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MasterServicePlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace MasterServicePlatform.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _db = db;
            _environment = environment;
        }

        // Profile visualization
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var profile = await _db.UserProfiles
                .Include(p => p.User)
                .ThenInclude(u => u.Master)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            var viewModel = profile ?? new UserProfile
            {
                FirstName = user.FirstName ?? user.Email.Split('@')[0],
                LastName = user.LastName ?? "",
                City = "",
                Phone = user.PhoneNumber ?? "",
                UserId = user.Id,
                User = user
            };

            return View(viewModel);
        }

        // Edit (get)
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            var profile = await _db.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            return View(profile ?? new UserProfile
            {
                FirstName = user.FirstName ?? user.Email.Split('@')[0],
                LastName = user.LastName ?? "",
                City = "",
                Phone = user.PhoneNumber,
                UserId = user.Id
            });
        }

        // Edit (post)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfile input, IFormFile? avatarFile)
        {
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            var user = await _userManager.GetUserAsync(User);

            var profile = await _db.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                profile = new UserProfile { UserId = user.Id };
                _db.UserProfiles.Add(profile);
            }

            // Updating profile
            profile.FirstName = input.FirstName;
            profile.LastName = input.LastName;
            profile.City = input.City;
            profile.Phone = input.Phone;

            // Sinchronizing with ApplicationUser
            user.FirstName = input.FirstName;
            user.LastName = input.LastName;
            user.PhoneNumber = input.Phone;

            // Saving picture if there was
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsRoot))
                {
                    Directory.CreateDirectory(uploadsRoot);
                }

                var ext = Path.GetExtension(avatarFile.FileName);
                var fileName = $"{user.Id}_{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsRoot, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                user.AvatarPath = $"/uploads/avatars/{fileName}";
            }

            await _db.SaveChangesAsync();
            await _userManager.UpdateAsync(user);

            TempData["ProfileSaved"] = true;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Delete()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Index");

            // Changing reviews of the user
            var userReviews = await _db.Reviews
                .Where(r => r.UserId == user.Id)
                .ToListAsync();

            foreach (var r in userReviews)
            {
                r.IsUserDeleted = true;
                r.UserId = null;
            }

            // Deleting avatar
            if (!string.IsNullOrEmpty(user.AvatarPath))
            {
                var avatarPhysicalPath = Path.Combine(
                    _environment.WebRootPath,
                    user.AvatarPath.TrimStart('/')
                );

                if (System.IO.File.Exists(avatarPhysicalPath))
                {
                    System.IO.File.Delete(avatarPhysicalPath);
                }
            }

            // Deleting order files
            var userOrders = await _db.Orders
                .Where(o => o.UserId == user.Id)
                .ToListAsync();

            foreach (var order in userOrders)
            {
                if (!string.IsNullOrEmpty(order.AttachmentPath))
                {
                    var attachmentPhysicalPath = Path.Combine(
                        _environment.WebRootPath,
                        order.AttachmentPath.TrimStart('/')
                    );

                    if (System.IO.File.Exists(attachmentPhysicalPath))
                    {
                        System.IO.File.Delete(attachmentPhysicalPath);
                    }
                }
            }


            // Deleting profile
            var profile = await _db.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile != null)
                _db.UserProfiles.Remove(profile);

            // Deleting user
            await _userManager.DeleteAsync(user);

            await _db.SaveChangesAsync();

            // Logout
            return Redirect("/Identity/Account/Logout");
        }



    }
}
