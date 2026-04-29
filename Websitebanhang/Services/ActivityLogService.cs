using System;
using System.Threading.Tasks;
using Websitebanhang.Data;
using Websitebanhang.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Websitebanhang.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(string action, string entityType, string? entityId, string? description);
    }

    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivityLogService(AppDbContext context, IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task LogAsync(string action, string entityType, string? entityId, string? description)
        {
            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext?.User);
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            var log = new AdminActivityLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                UserId = user?.Id,
                IpAddress = ipAddress,
                CreatedAt = DateTime.Now
            };

            _context.AdminActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
