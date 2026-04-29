using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminLogController : Controller
    {
        private readonly AppDbContext _context;

        public AdminLogController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, string type)
        {
            var logs = _context.AdminActivityLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                logs = logs.Where(l => l.Description.Contains(search) || l.Action.Contains(search));
            }

            if (!string.IsNullOrEmpty(type))
            {
                logs = logs.Where(l => l.EntityType == type);
            }

            ViewBag.Search = search;
            ViewBag.Type = type;
            ViewBag.EntityTypes = await _context.AdminActivityLogs.Select(l => l.EntityType).Distinct().ToListAsync();

            return View(await logs.ToListAsync());
        }
    }
}
