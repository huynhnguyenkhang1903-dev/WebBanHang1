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

        // ================= DANH SÁCH =================
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ================= CHI TIẾT =================
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View(order);
        }

        // ================= DOANH THU =================
        public async Task<IActionResult> Revenue(DateTime? from, DateTime? to)
        {
            var start = (from ?? DateTime.Now.AddDays(-30)).Date;
            var end = (to ?? DateTime.Now).Date.AddDays(1).AddSeconds(-1);

            var totalRevenue = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                            (o.IsPaid || o.Status == "Delivered"))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var ordersCount = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end)
                .CountAsync();

            var paidOrdersCount = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                            (o.IsPaid || o.Status == "Delivered"))
                .CountAsync();

            var revenueByDay = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                            (o.IsPaid || o.Status == "Delivered"))
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new DailyRevenue
                {
                    Date = g.Key,
                    Amount = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            var revenueByMonth = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                            (o.IsPaid || o.Status == "Delivered"))
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new MonthlyRevenue
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
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

        // ================= DUYỆT =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != "Pending")
            {
                TempData["Error"] = "Chỉ duyệt đơn đang chờ!";
                return RedirectToAction("Details", new { id });
            }

            order.Status = "Confirmed";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã duyệt đơn!";
            return RedirectToAction("Details", new { id });
        }

        // ================= GIAO =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Shipping(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != "Confirmed")
            {
                TempData["Error"] = "Phải duyệt trước!";
                return RedirectToAction("Details", new { id });
            }

            order.Status = "Shipping";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đang giao hàng!";
            return RedirectToAction("Details", new { id });
        }

        // ================= HOÀN THÀNH =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delivered(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != "Shipping")
            {
                TempData["Error"] = "Đơn chưa giao!";
                return RedirectToAction("Details", new { id });
            }

            order.Status = "Delivered";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã giao thành công!";
            return RedirectToAction("Details", new { id });
        }

        // ================= HỦY (CÓ LÝ DO) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string cancelReason)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn!";
                return RedirectToAction("Index");
            }

            if (order.Status == "Delivered")
            {
                TempData["Error"] = "Đơn đã giao, không thể hủy!";
                return RedirectToAction("Details", new { id });
            }

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                TempData["Error"] = "Phải nhập lý do hủy!";
                return RedirectToAction("Details", new { id });
            }

            order.Status = "Cancelled";
            order.CancelReason = cancelReason;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hủy đơn!";
            return RedirectToAction("Details", new { id });
        }

        // ================= TRẢ HÀNG =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReturn(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != OrderStatus.Delivered &&
                order.Status != OrderStatus.Returned)
            {
                TempData["Error"] = "Chỉ xử lý đơn đã giao!";
                return RedirectToAction("Details", new { id });
            }

            if (order.IsPaid &&
                string.Equals(order.PaymentMethod, "bank", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = OrderStatus.Refunded;
                order.IsPaid = false;
                order.TransactionId = null;
            }
            else
            {
                order.Status = OrderStatus.Returned;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xử lý trả hàng!";
            return RedirectToAction("Details", new { id });
        }

        // ================= IN HÓA ĐƠN =================
        [HttpGet]
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            return View("PrintInvoice", order);
        }
    }
}