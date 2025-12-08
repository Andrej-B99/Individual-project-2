using MasterServicePlatform.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MasterServicePlatform.Web.Controllers
{
    public class MastersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MastersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Masters
        public async Task<IActionResult> Index(string? search, string? sortOrder, string? profession, string? status)
        {
            var mastersQuery = _context.Masters
                .Include(m => m.Reviews)
                .AsQueryable();

            // Filtration according to search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                mastersQuery = mastersQuery.Where(m =>
                    m.FirstName.ToLower().Contains(search) ||
                    m.LastName.ToLower().Contains(search) ||
                    m.Profession.ToLower().Contains(search));
            }

            // Filtration according to profession
            if (!string.IsNullOrWhiteSpace(profession))
            {
                mastersQuery = mastersQuery.Where(m => m.Profession == profession);
            }

            // Filtration according to verification status
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<VerificationStatus>(status, out var parsedStatus))
            {
                mastersQuery = mastersQuery.Where(m => m.VerificationStatus == parsedStatus);
            }

            // Getting info from DB
            var masters = await mastersQuery.ToListAsync();

            // Sorting
            masters = sortOrder switch
            {
                "rating_desc" => masters.OrderByDescending(m => m.Reviews.Any() ? m.Reviews.Average(r => r.Rating) : 0).ToList(),
                "price_asc" => masters.OrderBy(m => m.PricePerHour).ToList(),
                "exp_desc" => masters.OrderByDescending(m => m.ExperienceYears).ToList(),
                _ => masters.OrderBy(m => m.FullName).ToList() // теперь безопасно
            };

            // Profession list
            ViewBag.Professions = await _context.Masters
                .Select(m => m.Profession)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            return View(masters);
        }

        // GET: Masters/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var master = await _context.Masters
                .Include(m => m.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (master == null)
                return NotFound();

            return View(master);
        }

        // GET: Masters/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Masters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Profession,ExperienceYears,PricePerHour,Phone,Email,Description,City,VerificationStatus")] Master master)
        {
            if (ModelState.IsValid)
            {
                _context.Add(master);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(master);
        }

        // GET: Masters/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var master = await _context.Masters.FindAsync(id);
            if (master == null)
                return NotFound();

            return View(master);
        }

        // POST: Masters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Profession,ExperienceYears,PricePerHour,Phone,Email,Description,City,VerificationStatus")] Master master)
        {
            if (id != master.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(master);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MasterExists(master.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(master);
        }

        // GET: Masters/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var master = await _context.Masters
                .Include(m => m.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (master == null)
                return NotFound();

            return View(master);
        }

        // POST: Masters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var master = await _context.Masters.FindAsync(id);
            if (master != null)
            {
                _context.Masters.Remove(master);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool MasterExists(int id)
        {
            return _context.Masters.Any(e => e.Id == id);
        }
    }
}
