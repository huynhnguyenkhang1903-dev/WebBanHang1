using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System.Threading.Tasks;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            IQueryable<Order> query = _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate);

            if (!isAdmin)
            {
                query = query.Where(o => o.UserId == user.Id);
            }

            var orders = await query.ToListAsync();
            return View(orders);
        }

        // Các hàm dưới giữ nguyên của bạn
    }
}