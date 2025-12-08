using MasterServicePlatform.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MasterServicePlatform.Web.Controllers
{
    [Authorize(Roles = "Master,Admin")]
    public class MasterProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public MasterProfileController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _db = db;
            _env = env;
        }

        // View master profile
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.MasterId == null)
                return RedirectToAction("Index", "Profile");

            var master = await _db.Masters
                .Include(m => m.Reviews)
                .Include(m => m.PortfolioPhotos)
                .FirstOrDefaultAsync(m => m.Id == user.MasterId);

            if (master == null)
                return NotFound();

            return View(master);
        }


        // Uploading portfolio (get)
        [HttpGet]
        [Authorize(Roles = "Master")]
        public IActionResult UploadPhoto()
        {
            return View();
        }


        // Uploading portfolio (post)
        [HttpPost]
        [Authorize(Roles = "Master")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "File is empty.";
                return RedirectToAction(nameof(UploadPhoto));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user.MasterId == null)
                return RedirectToAction("Index", "Profile");

            string folder = Path.Combine(_env.WebRootPath, "uploads/portfolio");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string extension = Path.GetExtension(file.FileName);
            string fileName = $"{Guid.NewGuid()}{extension}";
            string path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save to DB
            var photo = new PortfolioPhoto
            {
                MasterId = user.MasterId.Value,
                PhotoPath = $"/uploads/portfolio/{fileName}",
                UploadedAt = DateTime.Now
            };

            _db.PortfolioPhotos.Add(photo);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Photo uploaded successfully!";
            return RedirectToAction(nameof(Index));
        }


        // Edit profile (get)
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.MasterId == null)
                return RedirectToAction("Index", "Profile");

            var master = await _db.Masters.FirstOrDefaultAsync(m => m.Id == user.MasterId);
            if (master == null)
                return NotFound();

            return View(master);
        }


        // Edit profile (post)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Master input, IFormFile? AvatarFile)
        {
            if (!ModelState.IsValid)
                return View(input);

            var master = await _db.Masters.FindAsync(input.Id);
            if (master == null)
                return NotFound();

            // Update text fields
            master.FirstName = input.FirstName;
            master.LastName = input.LastName;
            master.City = input.City;
            master.Profession = input.Profession;
            master.ExperienceYears = input.ExperienceYears;
            master.PricePerHour = input.PricePerHour;
            master.Phone = input.Phone;
            master.Email = input.Email;
            master.Description = input.Description;

            var user = await _userManager.Users
        .FirstOrDefaultAsync(u => u.MasterId == master.Id);

            if (user != null)
            {
                user.FirstName = master.FirstName;
                user.LastName = master.LastName;
                await _userManager.UpdateAsync(user);
            }


            // Handle avatar photo
            if (AvatarFile != null && AvatarFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads/master-avatars");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Delete old file
                if (!string.IsNullOrEmpty(master.AvatarPath))
                {
                    string oldFile = Path.Combine(_env.WebRootPath, master.AvatarPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFile))
                        System.IO.File.Delete(oldFile);
                }

                string extension = Path.GetExtension(AvatarFile.FileName);
                string fileName = $"{Guid.NewGuid()}{extension}";
                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await AvatarFile.CopyToAsync(stream);
                }

                master.AvatarPath = $"/uploads/master-avatars/{fileName}";
            }

            await _db.SaveChangesAsync();
            TempData["Saved"] = true;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Index", "Home");

            if (user.MasterId == null)
                return RedirectToAction("Index", "Profile");

            // Searching for master files
            var master = await _db.Masters
                .Include(m => m.PortfolioPhotos)
                .FirstOrDefaultAsync(m => m.Id == user.MasterId);

            if (master == null)
                return RedirectToAction("Index", "Home");

            // Deleting portfolio files
            foreach (var photo in master.PortfolioPhotos)
            {
                if (!string.IsNullOrEmpty(photo.PhotoPath))
                {
                    var filePath = Path.Combine(_env.WebRootPath, photo.PhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
            }

            _db.PortfolioPhotos.RemoveRange(master.PortfolioPhotos);

            // Processing reviews of the master
            var userReviews = await _db.Reviews
                .Where(r => r.UserId == user.Id)
                .ToListAsync();

            foreach (var r in userReviews)
            {
                r.UserId = null;
                r.IsUserDeleted = true;
            }

            // Deleting avatar
            if (!string.IsNullOrEmpty(master.AvatarPath))
            {
                var avatarPhysical = Path.Combine(_env.WebRootPath, master.AvatarPath.TrimStart('/'));

                if (System.IO.File.Exists(avatarPhysical))
                    System.IO.File.Delete(avatarPhysical);
            }

            // Deleting orders
            var masterOrders = await _db.Orders
                .Where(o => o.UserId == user.Id)
                .ToListAsync();

            foreach (var order in masterOrders)
            {
                if (!string.IsNullOrEmpty(order.AttachmentPath))
                {
                    var attachmentPhysicalPath = Path.Combine(
                        _env.WebRootPath,
                        order.AttachmentPath.TrimStart('/')
                    );

                    if (System.IO.File.Exists(attachmentPhysicalPath))
                        System.IO.File.Delete(attachmentPhysicalPath);
                }
            }


            // Deleting master
            _db.Masters.Remove(master);

            // Saving changes in DB
            await _db.SaveChangesAsync();

            // Deleting user
            await _userManager.DeleteAsync(user);

            // Logout
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            // Going to main page
            return RedirectToAction("Index", "Home");
        }

    }
}
