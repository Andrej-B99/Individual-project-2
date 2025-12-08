using MasterServicePlatform.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;

namespace MasterServicePlatform.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Main page
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var masters = await _context.Masters
                .Include(m => m.Reviews)
                .Include(m => m.PortfolioPhotos)
                .ToListAsync();

            // Sort by rating
            masters = masters
                .OrderByDescending(m => m.AverageRating)
                .ToList();

            // Photo from top masters
            var topPhotos = masters
                .Where(m => m.PortfolioPhotos.Any())
                .OrderByDescending(m => m.AverageRating)
                .Take(4)
                .Select(m => m.PortfolioPhotos
                    .OrderByDescending(p => p.UploadedAt)
                    .First())
                .ToList();

            // Max 4 photo
            if (topPhotos.Count < 4)
            {
                int need = 4 - topPhotos.Count;

                // Getting new photos
                var additionalPhotos = masters
                    .SelectMany(m => m.PortfolioPhotos)
                    .OrderByDescending(p => p.UploadedAt)
                    .Where(p => !topPhotos.Contains(p))
                    .Take(need)
                    .ToList();

                topPhotos.AddRange(additionalPhotos);
            }

            ViewBag.TopPortfolioPhotos = topPhotos;

            if (User.Identity.IsAuthenticated && User.IsInRole("Admin"))
                return RedirectToAction("Index", "Admin");


            return View(masters);
        }


        // Admin dashboard
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Dashboard()
        {
            var masters = await _context.Masters
                .Include(m => m.Reviews)
                .ToListAsync();

            var totalMasters = masters.Count;
            var totalReviews = masters.Sum(m => m.Reviews.Count);

            var mastersWithReviews = masters.Where(m => m.Reviews.Any()).ToList();
            var averageRating = mastersWithReviews.Any()
                ? mastersWithReviews.Average(m => m.AverageRating)
                : 0;

            var topMasters = mastersWithReviews
                .OrderByDescending(m => m.AverageRating)
                .Take(3)
                .ToList();

            ViewBag.TotalMasters = totalMasters;
            ViewBag.TotalReviews = totalReviews;
            ViewBag.AverageRating = averageRating;
            ViewBag.TopMasters = topMasters;

            var reviewsByMonth = _context.Reviews
                .AsEnumerable()
                .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Month:D2}.{g.Key.Year}",
                    Count = g.Count()
                })
                .OrderBy(g => g.Month)
                .ToList();

            ViewBag.ReviewsByMonthLabels = reviewsByMonth
                .Select(r => r.Month)
                .ToList();

            ViewBag.ReviewsByMonthData = reviewsByMonth
                .Select(r => r.Count)
                .ToList();

            return View("Dashboard");
        }
    }
}
