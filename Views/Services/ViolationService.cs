using MasterServicePlatform.Web.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MasterServicePlatform.Web.Services
{
    public class ViolationService : IViolationService
    {
        private readonly ApplicationDbContext _db;

        public ViolationService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task AddViolationAsync(
            ApplicationUser user,
            ViolationType type,
            string? details = null,
            int? orderId = null,
            int? reviewId = null)
        {
            var violation = new UserViolation
            {
                UserId = user.Id,
                Type = type,
                Details = details,
                RelatedOrderId = orderId,
                RelatedReviewId = reviewId
            };

            _db.UserViolations.Add(violation);
            await _db.SaveChangesAsync();
        }

        public async Task<List<UserViolation>> GetUserViolationsAsync(string userId)
        {
            return await _db.UserViolations
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetViolationCountAsync(string userId)
        {
            return await _db.UserViolations
                .CountAsync(v => v.UserId == userId);
        }

        public async Task<bool> ShouldRecommendBlockAsync(string userId)
        {
            int count = await GetViolationCountAsync(userId);
            return count >= 5;
        }
    }
}
