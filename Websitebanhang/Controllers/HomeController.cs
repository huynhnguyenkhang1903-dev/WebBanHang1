using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System.Linq;
using Websitebanhang.Helpers;

namespace Websitebanhang.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 1. Get Newest Products (Top 6)
            var newProducts = _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .Take(6)
                .ToList();

            ViewBag.NewProducts = newProducts;

            // 2. Get Best Selling Products (Top 6)
            try 
            {
                var bestSellingProductIds = _context.Orders
                    .Include(o => o.Items)
                    .SelectMany(o => o.Items)
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(i => i.Quantity) })
                    .OrderByDescending(g => g.TotalQuantity)
                    .Take(6)
                    .Select(g => g.ProductId)
                    .ToList();

                var bestSellers = _context.Products
                    .Include(p => p.Category)
                    .Where(p => bestSellingProductIds.Contains(p.Id))
                    .ToList();

                if (!bestSellers.Any())
                {
                    bestSellers = _context.Products.Include(p => p.Category).Take(6).ToList();
                }

                ViewBag.BestSellers = bestSellers;
            }
            catch
            {
                ViewBag.BestSellers = _context.Products.Include(p => p.Category).Take(6).ToList();
            }

            // VOUCHERS: auto-generate some vouchers if not enough
            var activeVouchers = _context.Voucher
                .Where(v => v.ExpiryDate >= DateTime.Now)
                .OrderBy(v => v.ExpiryDate)
                .ToList();

            if (activeVouchers.Count < 6)
            {
                // create mix of order vouchers and shipping vouchers
                for (int i = 0; i < 6 - activeVouchers.Count; i++)
                {
                    var isShipping = (i % 2 == 0); // alternate
                    var prefix = isShipping ? "SHIP" : "ORD";
                    var code = prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                    var discount = isShipping ? 50 : 10; // shipping vouchers 50% off shipping, order vouchers 10%

                    var voucher = new Voucher
                    {
                        Code = code,
                        DiscountPercent = discount,
                        ExpiryDate = DateTime.Now.AddDays(7)
                    };
                    _context.Voucher.Add(voucher);
                }
                _context.SaveChanges();

                activeVouchers = _context.Voucher
                    .Where(v => v.ExpiryDate >= DateTime.Now)
                    .OrderBy(v => v.ExpiryDate)
                    .ToList();
            }

            ViewBag.Vouchers = activeVouchers;

            return View();
        }

        [HttpPost]
        public IActionResult ClaimVoucher(string code)
        {
            if (string.IsNullOrEmpty(code))
                return Json(new { success = false, message = "Mã không h?p l?" });

            var voucher = _context.Voucher.FirstOrDefault(v => v.Code == code && v.ExpiryDate >= DateTime.Now);
            if (voucher == null)
                return Json(new { success = false, message = "Mã không t?n t?i ho?c ?ã h?t h?n" });

            // store claimed vouchers in session for current visitor
            var claimed = HttpContext.Session.GetObject<List<string>>("ClaimedVouchers") ?? new List<string>();
            if (claimed.Contains(code))
                return Json(new { success = false, message = "B?n ?ã nh?n mã này" });

            claimed.Add(code);
            HttpContext.Session.SetObject("ClaimedVouchers", claimed);

            return Json(new { success = true, message = "?ã nh?n mã: " + code, code = code });
        }
    }
}