using MasterServicePlatform.Web.Infrastructure;
using MasterServicePlatform.Web.Models;
using MasterServicePlatform.Web.Models.ViewModels;
using MasterServicePlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MasterServicePlatform.Web.Controllers
{
    // View model for admin dashboard
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalMasters { get; set; }
        public int TotalOrders { get; set; }
        public int VerifiedMasters { get; set; }
        public int PendingMasters { get; set; }
        public int BlockedUsers { get; set; }
        public double AverageRating { get; set; }
    }

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IViolationService _violationService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IViolationService violationService)
        {
            _context = context;
            _userManager = userManager;
            _violationService = violationService;
        }

        // -------------------------------------------------------------------
        // DASHBOARD
        // -------------------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalMasters = await _context.Masters.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();

            var verifiedMasters = await _context.Masters
                .CountAsync(m => m.VerificationStatus == VerificationStatus.Verified);

            var pendingMasters = await _context.Masters
                .CountAsync(m => m.VerificationStatus == VerificationStatus.Pending);

            var blockedUsers = await _userManager.Users
                .CountAsync(u => u.IsBlocked);

            double averageRating = 0;
            if (await _context.Reviews.AnyAsync())
            {
                averageRating = await _context.Reviews.AverageAsync(r => r.Rating);
            }

            var model = new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalMasters = totalMasters,
                TotalOrders = totalOrders,
                VerifiedMasters = verifiedMasters,
                PendingMasters = pendingMasters,
                BlockedUsers = blockedUsers,
                AverageRating = System.Math.Round(averageRating, 2)
            };

            return View(model);
        }

        // -------------------------------------------------------------------
        // USERS LIST 
        // -------------------------------------------------------------------
        public async Task<IActionResult> Users(string search, string sort, string city, string violations)
        {
            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.City = city;
            ViewBag.Violations = violations;

            var users = await _context.Users
                .Where(u => u.Role == UserRole.User)
                .Include(u => u.Profile)
                .ToListAsync();


            var violationCounts = await _context.UserViolations
                .GroupBy(v => v.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.ViolationCounts = violationCounts;

            // -------------------------------------
            // SEARCH
            // -------------------------------------
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.ToLower();
                users = users.Where(u =>
                    (u.FirstName + " " + u.LastName).ToLower().Contains(s) ||
                    u.Email.ToLower().Contains(s)
                ).ToList();
            }

            // -------------------------------------
            // CITY FILTER (ИСПОЛЬЗУЕТ user.Profile.City)
            // -------------------------------------
            if (!string.IsNullOrEmpty(city))
            {
                users = users.Where(u =>
                    u.Profile != null &&
                    u.Profile.City != null &&
                    u.Profile.City == city
                ).ToList();
            }

            // -------------------------------------
            // VIOLATIONS FILTER
            // -------------------------------------
            if (!string.IsNullOrEmpty(violations))
            {
                if (violations == "0")
                    users = users.Where(u => !violationCounts.ContainsKey(u.Id)).ToList();

                if (violations == "1")
                    users = users.Where(u =>
                        violationCounts.ContainsKey(u.Id) &&
                        violationCounts[u.Id] > 0
                    ).ToList();
            }

            // -------------------------------------
            // SORTING
            // -------------------------------------
            users = sort switch
            {
                "name" => users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList(),
                "email" => users.OrderBy(u => u.Email).ToList(),
                "city" => users.OrderBy(u => u.Profile?.City).ToList(),
                "violations" => users.OrderByDescending(u =>
                    violationCounts.ContainsKey(u.Id) ? violationCounts[u.Id] : 0
                ).ToList(),
                _ => users
            };

            // -------------------------------------
            // CITY DROPDOWN LIST
            // -------------------------------------
            ViewBag.Cities = users
                .Where(u => u.Profile != null && !string.IsNullOrEmpty(u.Profile.City))
                .Select(u => u.Profile.City)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return View(users);
        }


        // -------------------------------------------------------------------
        // USER VIOLATIONS VIEW
        // -------------------------------------------------------------------
        public async Task<IActionResult> UserViolations(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var violations = await _violationService.GetUserViolationsAsync(id);

            ViewBag.User = user;

            return View(violations);
        }

        // -------------------------------------------------------------------
        // AJAX LOAD VIOLATIONS
        // -------------------------------------------------------------------
        public async Task<IActionResult> GetUserViolations(string id)
        {
            var violations = await _context.UserViolations
                .Where(v => v.UserId == id)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            if (!violations.Any())
                return Content("<div class='text-muted'>No violations found.</div>");

            string html = "<h6 class='fw-bold mb-2'>Violations:</h6><ul class='list-group'>";

            foreach (var v in violations)
            {
                html += $@"
                <li class='list-group-item'>
                    <b>{v.Type}</b> — {v.CreatedAt:dd.MM.yyyy HH:mm}<br/>
                    <small class='text-muted'>{v.Details}</small>
                </li>";
            }

            html += "</ul>";

            return Content(html, "text/html");
        }

        // -------------------------------------------------------------------
        // BLOCK / UNBLOCK
        // -------------------------------------------------------------------
        public async Task<IActionResult> BlockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            user.IsBlocked = true;
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Users");
        }

        public async Task<IActionResult> UnblockUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            user.IsBlocked = false;
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Users");
        }

        // -------------------------------------------------------------------
        // MASTERS LIST + Block + Violations
        // -------------------------------------------------------------------
        public async Task<IActionResult> Masters(
    string search,
    string status,
    string city,
    string violations,
    string sort)
        {
            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.City = city;
            ViewBag.Violations = violations;
            ViewBag.Sort = sort;

            // Loading masters
            var masters = await _userManager.Users
                .Where(u => u.Role == UserRole.Master)
                .Include(u => u.Master)
                .ToListAsync();

            // Violations
            var violationCounts = await _context.UserViolations
                .GroupBy(v => v.UserId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            ViewBag.ViolationCounts = violationCounts;

            // ---------------------------------------------------
            // SEARCH
            // ---------------------------------------------------
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.ToLower();
                masters = masters.Where(u =>
                    (u.FirstName + " " + u.LastName).ToLower().Contains(s) ||
                    u.Email.ToLower().Contains(s)
                ).ToList();
            }

            // ---------------------------------------------------
            // STATUS FILTER (Verified / Pending / Rejected)
            // ---------------------------------------------------
            if (!string.IsNullOrEmpty(status))
            {
                masters = status switch
                {
                    "verified" => masters.Where(u =>
                        u.Master!.VerificationStatus == VerificationStatus.Verified).ToList(),

                    "pending" => masters.Where(u =>
                        u.Master!.VerificationStatus == VerificationStatus.Pending ||
                        u.Master!.VerificationStatus == VerificationStatus.Unverified).ToList(),

                    "rejected" => masters.Where(u =>
                        u.Master!.VerificationStatus == VerificationStatus.Rejected).ToList(),

                    _ => masters
                };
            }

            // ---------------------------------------------------
            // CITY FILTER
            // ---------------------------------------------------
            if (!string.IsNullOrEmpty(city))
            {
                masters = masters.Where(u =>
                    u.Master != null &&
                    u.Master.City != null &&
                    u.Master.City == city
                ).ToList();
            }

            // ---------------------------------------------------
            // VIOLATIONS FILTER
            // ---------------------------------------------------
            if (!string.IsNullOrEmpty(violations))
            {
                masters = violations switch
                {
                    "0" => masters.Where(u => !violationCounts.ContainsKey(u.Id)).ToList(),

                    "1" => masters.Where(u =>
                        violationCounts.ContainsKey(u.Id) &&
                        violationCounts[u.Id] > 0
                    ).ToList(),

                    _ => masters
                };
            }

            // ---------------------------------------------------
            // SORTING
            // ---------------------------------------------------
            masters = sort switch
            {
                "name" => masters.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList(),
                "city" => masters.OrderBy(u => u.Master!.City).ToList(),
                "status" => masters.OrderBy(u => u.Master!.VerificationStatus).ToList(),
                _ => masters
            };

            // ---------------------------------------------------
            // CITY LIST FOR DROPDOWN
            // ---------------------------------------------------
            ViewBag.Cities = masters
                .Where(u => u.Master != null && !string.IsNullOrEmpty(u.Master.City))
                .Select(u => u.Master.City)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            return View(masters);
        }


        // -------------------------------------------------------------------
        // MASTER VERIFY / REJECT / RESET
        // -------------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> VerifyMaster(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.MasterId == null) return BadRequest();

            var master = await _context.Masters.FirstOrDefaultAsync(m => m.Id == user.MasterId);
            if (master == null) return NotFound();

            master.VerificationStatus = VerificationStatus.Verified;
            user.IsVerified = true;

            await _context.SaveChangesAsync();
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Masters");
        }

        [HttpPost]
        public async Task<IActionResult> RejectMaster(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.MasterId == null) return BadRequest();

            var master = await _context.Masters.FirstOrDefaultAsync(m => m.Id == user.MasterId);
            if (master == null) return NotFound();

            master.VerificationStatus = VerificationStatus.Rejected;
            user.IsVerified = false;

            await _context.SaveChangesAsync();
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Masters");
        }

        [HttpPost]
        public async Task<IActionResult> ResetMasterStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.MasterId == null) return BadRequest();

            var master = await _context.Masters.FirstOrDefaultAsync(m => m.Id == user.MasterId);
            if (master == null) return NotFound();

            master.VerificationStatus = VerificationStatus.Unverified;
            user.IsVerified = false;

            await _context.SaveChangesAsync();
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Masters");
        }

        public async Task<IActionResult> BlockMaster(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsBlocked = true;
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Masters");
        }

        public async Task<IActionResult> UnblockMaster(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            user.IsBlocked = false;
            await _userManager.UpdateAsync(user);

            return RedirectToAction("Masters");
        }


        // -------------------------------------------------------------------
        // MASTER DETAILS PAGE
        // -------------------------------------------------------------------
        public async Task<IActionResult> MasterDetails(int id)
        {
            var master = await _context.Masters
                .Include(m => m.PortfolioPhotos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (master == null) return NotFound();

            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.MasterId == id);

            var reviews = await _context.Reviews
                .Where(r => r.MasterId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var orders = await _context.Orders
                .Where(o => o.MasterId == id)
                .ToListAsync();

            var viewModel = new AdminMasterDetailsViewModel
            {
                Master = master,
                User = user,
                Reviews = reviews,
                Orders = orders,
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0
            };

            return View(viewModel);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _userManager.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound();

            var violations = await _violationService.GetUserViolationsAsync(id);

            var orders = await _context.Orders
                .Where(o => o.UserId == id)
                .Include(o => o.Master)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Include(r => r.Master)
                .Where(r => r.UserId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var model = new AdminUserDetailsViewModel
            {
                User = user,
                Violations = violations,
                Orders = orders,
                Reviews = reviews
            };

            

            return View(model);
        }


        public async Task<IActionResult> Orders(string status, string user, string master, string sort)
        {
            ViewBag.Status = status;
            ViewBag.User = user;
            ViewBag.Master = master;
            ViewBag.Sort = sort;

            // Loading orders
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Master)
                .ToListAsync();

            // FILTER: Status
            if (!string.IsNullOrEmpty(status))
                orders = orders.Where(o => o.Status.ToString() == status).ToList();

            // FILTER: User
            if (!string.IsNullOrEmpty(user))
                orders = orders.Where(o =>
                    (o.User.FirstName + " " + o.User.LastName).Contains(user, StringComparison.OrdinalIgnoreCase)
                ).ToList();

            // FILTER: Master
            if (!string.IsNullOrEmpty(master))
                orders = orders.Where(o =>
                    (o.Master.FirstName + " " + o.Master.LastName).Contains(master, StringComparison.OrdinalIgnoreCase)
                ).ToList();

            // SORT
            orders = sort switch
            {
                "date" => orders.OrderByDescending(o => o.CreatedAt).ToList(),
                "id" => orders.OrderBy(o => o.Id).ToList(),
                _ => orders
            };

            return View(orders);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Master)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        public async Task<IActionResult> Stats()
        {
            var model = new AdminStatsViewModel
            {
                UserCount = await _context.Users.CountAsync(),
                MasterCount = await _context.Masters.CountAsync(),
                OrderCount = await _context.Orders.CountAsync(),
                CompletedCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Completed),

                PendingCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                AcceptedCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Accepted),
                CompletedChartCount = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Completed),

                RecentOrders = await _context.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new RecentOrderItem
                    {
                        Id = o.Id,
                        User = o.User.FirstName + " " + o.User.LastName,
                        Master = o.Master != null
                            ? o.Master.FirstName + " " + o.Master.LastName
                            : "—",
                        Status = o.Status.ToString(),
                        CreatedAt = o.CreatedAt
                    })
                    .ToListAsync()

            };

            // Orders per day (last 7 days)
            var lastDays = DateTime.UtcNow.AddDays(-7);

            model.OrdersByDay = await _context.Orders
                .Where(o => o.CreatedAt >= lastDays)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new OrdersByDayItem
                {
                    Day = g.Key.ToString("dd.MM"),
                    Count = g.Count()
                })
                .ToListAsync();


            // Top masters
            model.TopMasters = await _context.Masters
                .Select(m => new TopMasterItem
                {
                    Id = m.Id,
                    Name = m.FirstName + " " + m.LastName,
                    Completed = _context.Orders.Count(o => o.MasterId == m.Id && o.Status == OrderStatus.Completed),
                    Rating = m.Reviews.Any()
                        ? m.Reviews.Average(r => r.Rating)
                        : 0

                })
                .OrderByDescending(m => m.Completed)
                .Take(5)
                .ToListAsync();


            return View(model);
        }



    }
}
