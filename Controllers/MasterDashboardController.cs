using MasterServicePlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MasterServicePlatform.Web.Controllers
{
    [Authorize(Roles = "Master")]
    public class MasterDashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public MasterDashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // MasterDashboard
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Saearching for master card
            var master = await _db.Masters
                .Include(m => m.Reviews)
                .FirstOrDefaultAsync(m => m.Id == user.MasterId);

            // If there is no, creating
            if (master == null) return RedirectToAction(nameof(CreateProfile));

            return View(master);
        }

        // GET: /MasterDashboard/CreateProfile
        [HttpGet]
        public IActionResult CreateProfile()
        {
            return View(new Master());
        }

        // POST: /MasterDashboard/CreateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(Master model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            _db.Masters.Add(model);
            await _db.SaveChangesAsync();

            user.MasterId = model.Id;
            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(Index));
        }
    }
}
