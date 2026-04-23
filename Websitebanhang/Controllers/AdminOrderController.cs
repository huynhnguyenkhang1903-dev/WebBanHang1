using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models.ViewModels;
using System.Globalization;
using Websitebanhang.Models;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrderController : Controller
    {
        private readonly AppDbContext _context;

        public AdminOrderController(AppDbContext context)
        {
            _context = context;
        }

        // 📋 Danh sách tất cả đơn hàng
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // NEW: Thống kê doanh thu
        public async Task<IActionResult> Revenue(DateTime? from, DateTime? to)
        {
            var start = (from ?? DateTime.Now.AddDays(-30)).Date;
            var end = (to ?? DateTime.Now).Date.AddDays(1).AddSeconds(-1);

            // Tổng doanh thu (tính chỉ những đơn đã thanh toán hoặc Delivered)
            var totalRevenue = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end && (o.IsPaid || o.Status == "Delivered"))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var ordersCount = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .CountAsync();

            var paidOrdersCount = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end && (o.IsPaid || o.Status == "Delivered"))
                .CountAsync();

            // Doanh thu theo ngày
            var revenueByDay = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end && (o.IsPaid || o.Status == "Delivered"))
                .GroupBy(o => new { d = o.OrderDate.Date })
                .Select(g => new DailyRevenue { Date = g.Key.d, Amount = g.Sum(x => x.TotalAmount) })
                .OrderBy(d => d.Date)
                .ToListAsync();

            // Doanh thu theo tháng (6 tháng gần nhất)
            var revenueByMonth = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end && (o.IsPaid || o.Status == "Delivered"))
                .GroupBy(o => new { y = o.OrderDate.Year, m = o.OrderDate.Month })
                .Select(g => new MonthlyRevenue { Year = g.Key.y, Month = g.Key.m, Amount = g.Sum(x => x.TotalAmount) })
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToListAsync();

            var model = new RevenueViewModel
            {
                TotalRevenue = totalRevenue,
                OrdersCount = ordersCount,
                PaidOrdersCount = paidOrdersCount,
                RevenueByDay = revenueByDay,
                RevenueByMonth = revenueByMonth
            };

            ViewBag.From = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ViewBag.To = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return View(model);
        }

        // ✅ DUYỆT ĐƠN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            if (order.Status != "Pending")
            {
                TempData["Error"] = "Chỉ duyệt đơn đang chờ!";
                return RedirectToAction("Index");
            }

            order.Status = "Confirmed";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã duyệt đơn!";
            return RedirectToAction("Index");
        }

        // 🚚 GIAO HÀNG
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Shipping(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            if (order.Status != "Confirmed")
            {
                TempData["Error"] = "Phải duyệt trước!";
                return RedirectToAction("Index");
            }

            order.Status = "Shipping";
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // 📦 HOÀN THÀNH
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delivered(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            if (order.Status != "Shipping")
            {
                TempData["Error"] = "Đơn chưa giao!";
                return RedirectToAction("Index");
            }

            order.Status = "Delivered";
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ❌ HỦY ĐƠN (ADMIN)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            if (order.Status == "Delivered")
            {
                TempData["Error"] = "Đơn đã giao, không thể hủy!";
                return RedirectToAction("Index");
            }

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // ADMIN: Xử lý trả hàng / hoàn tiền
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReturn(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Only process returns for Returned (requested) or Delivered states
            if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Returned)
            {
                TempData["Error"] = "Chỉ xử lý trả hàng cho đơn đã giao hoặc đã yêu cầu trả hàng.";
                return RedirectToAction("Index");
            }

            // If the return was requested by user (ReturnReason present) or admin decides, process refund if needed
            if (order.IsPaid && string.Equals(order.PaymentMethod, "bank", StringComparison.OrdinalIgnoreCase))
            {
                // perform refund simulation
                order.Status = OrderStatus.Refunded;
                order.IsPaid = false;
                order.TransactionId = null;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã hoàn tiền cho đơn (chuyển khoản).";
                return RedirectToAction("Index");
            }

            // Otherwise mark as Returned
            order.Status = OrderStatus.Returned;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã ghi nhận trả hàng.";
            return RedirectToAction("Index");
        }
    }
}