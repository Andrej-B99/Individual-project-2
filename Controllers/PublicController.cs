using MasterServicePlatform.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MasterServicePlatform.Web.Controllers
{
    public class PublicController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PublicController(ApplicationDbContext context)
        {
            _context = context;
        }

        // List of masters
        public async Task<IActionResult> Index(
    string? search,
    string? profession,
    string? city,
    int? minPrice,
    int? maxPrice,
    string? experience,
    int? rating,
    bool? verified,
    string? sort)
        {
            var mastersQuery = _context.Masters
                .Include(m => m.Reviews)
                .AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();

                mastersQuery = mastersQuery.Where(m =>
                    (m.FirstName != null && m.FirstName.ToLower().Contains(term)) ||
                    (m.LastName != null && m.LastName.ToLower().Contains(term)) ||
                    (m.Profession != null && m.Profession.ToLower().Contains(term)) ||
                    (m.City != null && m.City.ToLower().Contains(term)) ||
                    (m.Description != null && m.Description.ToLower().Contains(term))
                );
            }

            // PROFESSION
            if (!string.IsNullOrWhiteSpace(profession))
                mastersQuery = mastersQuery.Where(m => m.Profession == profession);

            // CITY
            if (!string.IsNullOrWhiteSpace(city))
                mastersQuery = mastersQuery.Where(m => m.City == city);

            // PRICE MIN/MAX
            if (minPrice.HasValue)
                mastersQuery = mastersQuery.Where(m => m.PricePerHour >= minPrice.Value);

            if (maxPrice.HasValue)
                mastersQuery = mastersQuery.Where(m => m.PricePerHour <= maxPrice.Value);

            // EXPERIENCE (только если поле существует в БД!)
            if (!string.IsNullOrWhiteSpace(experience))
            {
                switch (experience)
                {
                    case "1-3":
                        mastersQuery = mastersQuery.Where(m => m.ExperienceYears >= 1 && m.ExperienceYears <= 3);
                        break;

                    case "3-5":
                        mastersQuery = mastersQuery.Where(m => m.ExperienceYears >= 3 && m.ExperienceYears <= 5);
                        break;

                    case "5+":
                        mastersQuery = mastersQuery.Where(m => m.ExperienceYears >= 5);
                        break;
                }
            }

            // VERIFIED
            if (verified.HasValue)
            {
                if (verified.Value)
                    mastersQuery = mastersQuery.Where(m => m.VerificationStatus == VerificationStatus.Verified);
                else
                    mastersQuery = mastersQuery.Where(m => m.VerificationStatus != VerificationStatus.Verified);
            }

            var list = await mastersQuery.ToListAsync();

            // Rating
            if (rating.HasValue)
                list = list.Where(m => m.AverageRating >= rating.Value).ToList();

            // Sorting
            list = sort switch
            {
                "rating" => list.OrderByDescending(m => m.AverageRating).ToList(),
                "priceAsc" => list.OrderBy(m => m.PricePerHour).ToList(),
                "priceDesc" => list.OrderByDescending(m => m.PricePerHour).ToList(),
                "newest" => list.OrderByDescending(m => m.Id).ToList(),
                _ => list.OrderBy(m => m.FullName).ToList()
            };

            // Dropdowns
            ViewBag.Professions = await _context.Masters
                .Select(m => m.Profession)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            ViewBag.Cities = await _context.Masters
                .Select(m => m.City)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return View(list);
        }



        // Profile check
        public async Task<IActionResult> Details(int id)
        {
            var master = await _context.Masters
                .Include(m => m.Reviews)
                .ThenInclude(r => r.User)
                .Include(m => m.PortfolioPhotos)
                .FirstOrDefaultAsync(m => m.Id == id);


            if (master == null)
                return NotFound();

            return View(master);
        }

        public async Task<IActionResult> Portfolio(int id)
        {
            var master = await _context.Masters
                .Include(m => m.PortfolioPhotos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (master == null)
                return NotFound();

            return View(master);
        }

    }
}
