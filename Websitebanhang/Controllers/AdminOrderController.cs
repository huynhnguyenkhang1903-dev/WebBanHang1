using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;

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
    }
}