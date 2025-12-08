using System.Collections.Generic;
using System.Threading.Tasks;
using MasterServicePlatform.Web.Models;

namespace MasterServicePlatform.Web.Services
{
    public interface IViolationService
    {
        Task AddViolationAsync(
            ApplicationUser user,
            ViolationType type,
            string? details = null,
            int? orderId = null,
            int? reviewId = null);

        Task<List<UserViolation>> GetUserViolationsAsync(string userId);

        Task<int> GetViolationCountAsync(string userId);

        Task<bool> ShouldRecommendBlockAsync(string userId);
    }
}
