<<<<<<< HEAD
﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
=======
using Microsoft.AspNetCore.Authorization;
>>>>>>> bf5665fb560577c9cb7231b7c9f72f71195e6fcd
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using System.Threading.Tasks;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // ================= DANH SÁCH ĐƠN =================
        public async Task<IActionResult> Index()
        {
<<<<<<< HEAD
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return RedirectToAction("Login", "Account");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            IQueryable<Order> query = _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate);

            // 👤 User thường chỉ thấy đơn của mình
            if (!isAdmin)
            {
                query = query.Where(o => o.UserId == user.Id);
            }

            var orders = await query.ToListAsync();
=======
            var orders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
>>>>>>> bf5665fb560577c9cb7231b7c9f72f71195e6fcd

            return View(orders);
        }

        // ================= DUYỆT ĐƠN =================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

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

        // ================= GIAO HÀNG =================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Shipping(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != "Confirmed")
            {
                TempData["Error"] = "Phải duyệt trước!";
                return RedirectToAction("Index");
            }

            order.Status = "Shipping";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đơn đang giao!";
            return RedirectToAction("Index");
        }

        // ================= HOÀN THÀNH =================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delivered(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != "Shipping")
            {
                TempData["Error"] = "Đơn chưa giao!";
                return RedirectToAction("Index");
            }

            order.Status = "Delivered";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hoàn thành!";
            return RedirectToAction("Index");
        }

        // ================= HỦY ĐƠN (ADMIN) =================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status == "Delivered")
            {
                TempData["Error"] = "Đơn đã giao không thể hủy!";
                return RedirectToAction("Index");
            }

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hủy đơn!";
            return RedirectToAction("Index");
        }
    }
}