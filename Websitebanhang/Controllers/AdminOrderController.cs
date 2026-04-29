using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models.ViewModels;
using System.Globalization;
using Websitebanhang.Models;
using Websitebanhang.Services;
using Microsoft.AspNetCore.SignalR;
using Websitebanhang.Hubs;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AdminOrderController(AppDbContext context, IEmailService emailService, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _emailService = emailService;
            _hubContext = hubContext;
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
                    Amount = g.Sum(x => x.TotalAmount),
                    OrderCount = g.Count()
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
                    Amount = g.Sum(x => x.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            var topProducts = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                            (o.IsPaid || o.Status == "Delivered"))
                .Include(o => o.Items)
                .SelectMany(o => o.Items ?? new List<CartItem>())
                .GroupBy(i => new { i.ProductId, i.Name })
                .Select(g => new TopSellingProduct
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Quantity * i.Price)
                })
                .OrderByDescending(p => p.TotalQuantity)
                .Take(10)
                .ToListAsync();

            var revenueByYear = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                            (o.IsPaid || o.Status == "Delivered"))
                .GroupBy(o => o.OrderDate.Year)
                .Select(g => new YearlyRevenue
                {
                    Year = g.Key,
                    Amount = g.Sum(x => x.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderBy(x => x.Year)
                .ToListAsync();

            var model = new RevenueViewModel
            {
                TotalRevenue = totalRevenue,
                OrdersCount = ordersCount,
                PaidOrdersCount = paidOrdersCount,
                RevenueByDay = revenueByDay,
                RevenueByMonth = revenueByMonth,
                RevenueByYear = revenueByYear,
                TopProducts = topProducts
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

            // --- GỬI EMAIL XÁC NHẬN KHI ADMIN DUYỆT ĐƠN ---
            try
            {
                if (!string.IsNullOrEmpty(order.Email))
                {
                    string emailBody = $@"
                        <h2>Xác nhận duyệt đơn hàng #{order.Id}</h2>
                        <p>Chào {order.CustomerName},</p>
                        <p>Đơn hàng của bạn đã được cửa hàng xác nhận và đang trong quá trình chuẩn bị.</p>
                        <h3>Chi tiết đơn hàng:</h3>
                        <ul>
                            <li><strong>Tổng tiền:</strong> {order.TotalAmount:N0} ₫</li>
                            <li><strong>Phương thức thanh toán:</strong> {(order.PaymentMethod == "bank" ? "Chuyển khoản ngân hàng" : "Thanh toán khi nhận hàng (COD)")}</li>
                            <li><strong>Địa chỉ giao hàng:</strong> {order.Address}</li>
                        </ul>
                        <p>Chúng tôi sẽ thông báo cho bạn khi đơn hàng bắt đầu được giao.</p>
                        <br/>
                        <p>Trân trọng,</p>
                        <p><strong>Đội ngũ Coffee Shop</strong></p>
                    ";
                    await _emailService.SendEmailAsync(order.Email, $"Coffee Shop - Đơn hàng #{order.Id} đã được duyệt", emailBody);
                }
            }
            catch
            {
                // Bỏ qua lỗi gửi email để không chặn luồng duyệt đơn
            }
            // ------------------------------------------------

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Đơn hàng #{order.Id} của bạn đã được duyệt!", $"/Account/OrderDetails/{order.Id}");
            }

            TempData["Success"] = "Đã duyệt đơn và gửi email xác nhận!";
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

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Đơn hàng #{order.Id} đang trên đường giao đến bạn!", $"/Account/OrderDetails/{order.Id}");
            }

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

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Đơn hàng #{order.Id} đã giao thành công!", $"/Account/OrderDetails/{order.Id}");
            }

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

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Đơn hàng #{order.Id} đã bị hủy. Lý do: {cancelReason}", $"/Account/OrderDetails/{order.Id}");
            }

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

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Yêu cầu trả hàng cho đơn #{order.Id} đã được xử lý.", $"/Account/OrderDetails/{order.Id}");
            }

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

        // ================= CẬP NHẬT TRẠNG THÁI =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var oldStatus = order.Status;
            order.Status = newStatus;

            if (newStatus == OrderStatus.Cancelled && string.IsNullOrEmpty(order.CancelReason))
            {
                order.CancelReason = "Admin cập nhật trạng thái sang Hủy";
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Đơn hàng #{order.Id} đã chuyển sang: {newStatus}", $"/Account/OrderDetails/{order.Id}");
            }

            TempData["Success"] = $"Đã cập nhật trạng thái từ {oldStatus} sang {newStatus}!";
            return RedirectToAction("Details", new { id });
        }
    }
}