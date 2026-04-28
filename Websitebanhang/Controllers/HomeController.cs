using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System.Linq;
using Websitebanhang.Helpers;
using System;

namespace Websitebanhang.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IActionResult Index()
        {
            var newProducts = _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .Take(6)
                .ToList();

            ViewBag.NewProducts = newProducts;

            try
            {
                var bestSellingProductIds = _context.Orders
                    .Include(o => o.Items)
                    .SelectMany(o => o.Items ?? new List<CartItem>())
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
                    bestSellers = _context.Products
                        .Include(p => p.Category)
                        .Take(6)
                        .ToList();
                }

                ViewBag.BestSellers = bestSellers;
            }
            catch
            {
                ViewBag.BestSellers = _context.Products
                    .Include(p => p.Category)
                    .Take(6)
                    .ToList();
            }

            // Products on promotion (has valid voucher)
            var promoProducts = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Voucher)
                .Where(p => p.VoucherId != null && p.Voucher != null && p.Voucher.ExpiryDate >= DateTime.Now)
                .OrderByDescending(p => p.Id)
                .Take(6)
                .ToList();
            
            ViewBag.PromoProducts = promoProducts;

            var activeVouchers = _context.Voucher
                .Where(v => v.ExpiryDate >= DateTime.Now)
                .OrderBy(v => v.ExpiryDate)
                .ToList();

            if (activeVouchers.Count < 6)
            {
                for (int i = 0; i < 6 - activeVouchers.Count; i++)
                {
                    var isShipping = (i % 2 == 0);
                    var prefix = isShipping ? "SHIP" : "ORD";
                    var code = prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();

                    var voucher = new Voucher
                    {
                        Code = code,
                        DiscountPercent = isShipping ? 50 : 10,
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
                return Json(new { success = false, message = "Mã không hợp lệ" });

            var voucher = _context.Voucher
                .FirstOrDefault(v => v.Code == code && v.ExpiryDate >= DateTime.Now);

            if (voucher == null)
                return Json(new { success = false, message = "Mã không tồn tại hoặc đã hết hạn" });

            // FIX SESSION
            var session = HttpContext.Session;

            if (session == null)
                return Json(new { success = false, message = "Session chưa được cấu hình" });

            var claimed = session.GetObject<List<string>>("ClaimedVouchers") ?? new List<string>();

            if (claimed.Contains(code))
                return Json(new { success = false, message = "Bạn đã nhận mã này" });

            claimed.Add(code);
            session.SetObject("ClaimedVouchers", claimed);

            return Json(new
            {
                success = true,
                message = "Đã nhận mã: " + code,
                code = code
            });
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
}