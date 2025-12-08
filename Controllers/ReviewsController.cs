using MasterServicePlatform.Web.Models;
using MasterServicePlatform.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MasterServicePlatform.Web.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IViolationService _violationService;

        public ReviewsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IViolationService violationService)
        {
            _context = context;
            _userManager = userManager;
            _violationService = violationService;
        }

        // POST: Reviews/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MasterId,Rating,Comment")] Review review)
        {
            if (!User.Identity.IsAuthenticated)
                return Unauthorized();

            // Fake Review detection
            var user = await _userManager.GetUserAsync(User);

            // Checking completed orders
            bool hasCompletedOrder = await _context.Orders
                .AnyAsync(o =>
                    o.UserId == user.Id &&
                    o.MasterId == review.MasterId &&
                    o.Status == OrderStatus.Completed);

            if (!hasCompletedOrder)
            {
                // Detecting violation
                await _violationService.AddViolationAsync(
                    user,
                    ViolationType.FakeReview,
                    $"User attempted to post review without completed orders with master {review.MasterId}.",
                    reviewId: null
                );

                TempData["FakeReviewError"] = "You cannot leave a review for this master because you have no completed orders with them.";

                return RedirectToAction("Details", "Public", new { id = review.MasterId });
            }


            if (ModelState.IsValid)
            {
                review.UserId = _userManager.GetUserId(User);
                review.CreatedAt = DateTime.UtcNow;

                //Review Flood detection 
                var now = DateTime.UtcNow;
                var windowStart = now.AddMinutes(-10);

                var recentReviewsCount = await _context.Reviews
                    .Where(r => r.UserId == user.Id && r.CreatedAt >= windowStart)
                    .CountAsync();

                if (recentReviewsCount > 3)
                {
                    TempData["ReviewSpamWarning"] = "You are posting reviews too frequently. Please slow down.";
                    await _violationService.AddViolationAsync(
                        user,
                        ViolationType.ReviewFlood,
                        $"User posted {recentReviewsCount + 1} reviews in 10 minutes.",
                        reviewId: review.Id
                    );
                    return RedirectToAction("Details", "Public", new { id = review.MasterId });
                }

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                return RedirectToAction("Details", "Public", new { id = review.MasterId });
            }

            return RedirectToAction("Details", "Public", new { id = review.MasterId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Rating,Comment")] Review form)
        {
            var review = await _context.Reviews
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (review.UserId != userId)
                return Forbid();

            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-10);

            bool ratingChanged = review.Rating != form.Rating;

            bool alreadyViolated = await _context.UserViolations
                .AnyAsync(v =>
                    v.UserId == userId &&
                    v.Type == ViolationType.RatingAbuse &&
                    v.CreatedAt >= windowStart);

            if (ratingChanged && alreadyViolated)
            {
                TempData["ReviewEditBlocked"] =
                    "You are changing your rating too frequently. Please try again later.";

                await _violationService.AddViolationAsync(
                    review.User,
                    ViolationType.RatingAbuse,
                    $"User attempted rating abuse again for master {review.MasterId}.",
                    reviewId: review.Id
                );

                return RedirectToAction("Details", "Public", new { id = review.MasterId });
            }

            if (ratingChanged && !alreadyViolated)
            {
                await _violationService.AddViolationAsync(
                    review.User,
                    ViolationType.RatingAbuse,
                    $"User changed rating for master {review.MasterId}.",
                    reviewId: review.Id
                );
            }


            // Save actual change
            review.Rating = form.Rating;
            review.Comment = form.Comment;
            review.LastEditedAt = now;

            await _context.SaveChangesAsync();


            return RedirectToAction("Details", "Public", new { id = review.MasterId });
        }




        // POST: Reviews/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
                return NotFound();

            if (review.UserId != _userManager.GetUserId(User))
                return Forbid();

            var masterId = review.MasterId;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Public", new { id = masterId });
        }
    }
}
