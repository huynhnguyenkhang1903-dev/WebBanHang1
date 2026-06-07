using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var lowStockProducts = await _context.Products
                .Where(p => p.Stock <= 10)
                .ToListAsync();

            var expiringProducts = await _context.Products
                .Where(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value <= DateTime.Now.AddDays(30))
                .ToListAsync();

            ViewBag.LowStockProducts = lowStockProducts;
            ViewBag.ExpiringProducts = expiringProducts;

            return View();
        }
    }
}