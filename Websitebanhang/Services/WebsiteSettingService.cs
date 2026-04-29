using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;

namespace Websitebanhang.Services
{
    public interface IWebsiteSettingService
    {
        Task<string> GetSettingAsync(string key, string defaultValue = "");
    }

    public class WebsiteSettingService : IWebsiteSettingService
    {
        private readonly AppDbContext _context;

        public WebsiteSettingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            var setting = await _context.WebsiteSettings.FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value ?? defaultValue;
        }
    }
}
