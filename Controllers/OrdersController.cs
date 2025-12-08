using MasterServicePlatform.Web.Models;
using MasterServicePlatform.Web.Models.ViewModels;
using MasterServicePlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace MasterServicePlatform.Web.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IViolationService _violationService;

        public OrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IViolationService violationService)
        {
            _context = context;
            _userManager = userManager;
            _violationService = violationService;
        }


        // GET: /Orders/Create?masterId=5
        [HttpGet]
        public IActionResult Create(int masterId)
        {
            var model = new CreateOrderViewModel
            {
                MasterId = masterId
            };

            return View(model);
        }

        // POST: /Orders/Create
        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            // CHECK FOR ORDER SPAM: >3 orders in last 10 minutes
            var tenMinutesAgo = DateTime.Now.AddMinutes(-10);

            var recentOrdersCount = await _context.Orders
                .Where(o => o.UserId == user.Id && o.CreatedAt >= tenMinutesAgo)
                .CountAsync();

            if (recentOrdersCount >= 3)
            {
                TempData["OrderSpamWarning"] = "You are creating orders too frequently. Please wait before making another one.";

                // NEW: log violation
                await _violationService.AddViolationAsync(
                    user,
                    ViolationType.OrderSpam,
                    $"Attempted to create {recentOrdersCount + 1} orders in 10 minutes.");

                return RedirectToAction("MyOrders");
            }


            string attachmentPath = null;

            // If file uploaded saving
            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/orders");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}_{model.Attachment.FileName}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Attachment.CopyToAsync(stream);
                }

                attachmentPath = $"/uploads/orders/{fileName}";
            }

            var order = new Order
            {
                UserId = user.Id,
                MasterId = model.MasterId,
                Description = model.Description,
                Address = model.Address,
                Budget = model.Budget,
                PreferredDate = model.PreferredDate,
                PreferredTime = model.PreferredTime,
                ServiceCategory = model.ServiceCategory,
                AttachmentPath = attachmentPath,
                CreatedAt = DateTime.Now,
                Status = OrderStatus.Pending
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();



            return RedirectToAction("MyOrders");
        }




        // GET: /Orders/ForMaster
        [Authorize]
        public async Task<IActionResult> ForMaster()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user.MasterId == null)
                return RedirectToAction("Index", "Home");

            var orders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.MasterId == user.MasterId)
                .OrderBy(o => o.Status)
                .ThenByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // GET: /Orders/MyOrders
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);

            var orders = await _context.Orders
                .Include(o => o.Master)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }


        [HttpPost]
        public async Task<IActionResult> Accept(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = OrderStatus.Accepted;
            await _context.SaveChangesAsync();

            return RedirectToAction("ForMaster");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = OrderStatus.Rejected;
            await _context.SaveChangesAsync();

            return RedirectToAction("ForMaster");
        }

        [HttpPost]
        public async Task<IActionResult> Complete(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.Status = OrderStatus.Completed;
            await _context.SaveChangesAsync();

            return RedirectToAction("ForMaster");
        }

    }
}
