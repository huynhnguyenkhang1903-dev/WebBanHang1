using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System.Threading.Tasks;
using Websitebanhang.Services;
using System;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public OrderController(AppDbContext context, UserManager<ApplicationUser> userManager, IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CapNhatTrangThai(int id, string trangThai)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            order.Status = trangThai;

            // If admin changes to "Approved", confirm payment immediately
            if (trangThai == "Approved" && !order.IsPaid && order.PaymentMethod == "bank")
            {
                order.IsPaid = true;
                order.PaidAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Send a notification email to the customer
            string tieuDe = $"Aura Coffee - Update order #ORD-{order.Id:D3}";

            string noiDung = $@"
                <h2>Hello {order.CustomerName},</h2>
                <p>Your order <strong>#ORD-{order.Id:D3}</strong> has been updated:</p>
                <p style='font-size:20px;color:#6F4E37'><strong>Status: {trangThai}</strong></p>
                <p>Total: {order.TotalAmount:N0}đ</p>
                <br><p>Thank you for shopping at Aura Coffee!</p>";

            await _emailService.SendEmailAsync(order.Email, tieuDe, noiDung);

            TempData["Success"] = $"Order #{order.Id} has been updated to '{trangThai}'";

            return RedirectToAction("Index");
        }
    }
}