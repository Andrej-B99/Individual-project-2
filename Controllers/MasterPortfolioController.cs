using MasterServicePlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MasterServicePlatform.Web.Controllers
{
    [Authorize(Roles = "Master")]
    public class MasterPortfolioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MasterPortfolioController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            var photos = _context.PortfolioPhotos
                .Where(p => p.MasterId == user.MasterId)
                .OrderByDescending(p => p.UploadedAt)
                .ToList();

            return View(photos);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile photo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user.MasterId == null)
                return Unauthorized();

            if (photo == null || photo.Length == 0)
                return RedirectToAction("Index");

            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/portfolio");
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid()}_{photo.FileName}";
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            var entity = new PortfolioPhoto
            {
                MasterId = user.MasterId.Value,
                PhotoPath = $"/uploads/portfolio/{fileName}",
                UploadedAt = DateTime.Now
            };

            _context.PortfolioPhotos.Add(entity);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.MasterId == null)
                return Unauthorized();

            // Searching for photo
            var photo = await _context.PortfolioPhotos
                .FirstOrDefaultAsync(p => p.Id == id && p.MasterId == user.MasterId);

            if (photo == null)
                return NotFound();

            // Path to the file
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photo.PhotoPath.TrimStart('/'));

            // Deletion of the file
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Deleting from DB
            _context.PortfolioPhotos.Remove(photo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Photo deleted.";

            return RedirectToAction("Index", "MasterProfile");
        }

    }
}
