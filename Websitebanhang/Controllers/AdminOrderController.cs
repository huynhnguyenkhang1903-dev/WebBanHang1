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
        private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;
        private readonly IActivityLogService _activityLogService;

        public AdminOrderController(AppDbContext context, IEmailService emailService, IHubContext<NotificationHub> hubContext, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager, IActivityLogService activityLogService)
        {
            _context = context;
            _emailService = emailService;
            _hubContext = hubContext;
            _userManager = userManager;
            _activityLogService = activityLogService;
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

            var revenueByDay = (await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                            (o.IsPaid || o.Status == "Delivered"))
                .ToListAsync())
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new DailyRevenue
                {
                    Date = g.Key,
                    Amount = g.Sum(x => x.TotalAmount),
                    OrderCount = g.Count(),
                    ProductsSold = g.SelectMany(x => x.Items ?? new List<CartItem>()).Sum(i => i.Quantity)
                })
                .OrderBy(x => x.Date)
                .ToList();

            var revenueByMonth = (await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                            (o.IsPaid || o.Status == "Delivered"))
                .ToListAsync())
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new MonthlyRevenue
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Amount = g.Sum(x => x.TotalAmount),
                    OrderCount = g.Count(),
                    ProductsSold = g.SelectMany(x => x.Items ?? new List<CartItem>()).Sum(i => i.Quantity)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToList();

            var topProducts = (await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                            (o.IsPaid || o.Status == "Delivered"))
                .ToListAsync())
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
                .ToList();

            var revenueByYear = (await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                            (o.IsPaid || o.Status == "Delivered"))
                .ToListAsync())
                .GroupBy(o => o.OrderDate.Year)
                .Select(g => new YearlyRevenue
                {
                    Year = g.Key,
                    Amount = g.Sum(x => x.TotalAmount),
                    OrderCount = g.Count(),
                    ProductsSold = g.SelectMany(x => x.Items ?? new List<CartItem>()).Sum(i => i.Quantity)
                })
                .OrderBy(x => x.Year)
                .ToList();

            var totalProductsSold = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end &&
                            (o.IsPaid || o.Status == "Delivered"))
                .SelectMany(o => o.Items)
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            var model = new RevenueViewModel
            {
                TotalRevenue = totalRevenue,
                OrdersCount = ordersCount,
                PaidOrdersCount = paidOrdersCount,
                TotalProductsSold = totalProductsSold,
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

            // 🔥 TRỪ TỒN KHO KHI DUYỆT ĐƠN
            foreach (var item in order.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock -= item.Quantity;
                    
                    // Log history
                    _context.StockHistories.Add(new StockHistory
                    {
                        ProductId = product.Id,
                        QuantityChange = -item.Quantity,
                        BalanceAfter = product.Stock,
                        Type = "Xuất kho (Bán hàng)",
                        Note = $"Đơn hàng #{order.Id}",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            // --- GỬI EMAIL XÁC NHẬN KHI ADMIN DUYỆT ĐƠN ---
            await SendStatusEmailAsync(order, "Đã xác nhận");
            // ------------------------------------------------

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Đơn hàng #{order.Id} của bạn đã được duyệt!", $"/Account/OrderDetails/{order.Id}");
            }

            TempData["Success"] = "Đã duyệt đơn và gửi email xác nhận!";
            return RedirectToAction("Details", new { id });
        }

        // ================= ĐÓNG GÓI =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preparing(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != "Confirmed")
            {
                TempData["Error"] = "Chưa duyệt đơn!";
                return RedirectToAction("Details", new { id });
            }

            order.Status = "Preparing";
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Đơn hàng #{order.Id} của bạn đang được đóng gói!", $"/Account/OrderDetails/{order.Id}");
            }

            TempData["Success"] = "Đã chuyển sang trạng thái Đóng gói!";
            return RedirectToAction("Details", new { id });
        }

        // ================= GIAO =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Shipping(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != "Preparing" && order.Status != "Confirmed")
            {
                TempData["Error"] = "Phải duyệt hoặc đóng gói trước!";
                return RedirectToAction("Details", new { id });
            }

            order.Status = "Shipping";
            await _context.SaveChangesAsync();
            await _activityLogService.LogAsync("Giao hàng", "Order", order.Id.ToString(), $"Admin đã chuyển đơn hàng #{order.Id} sang Đang giao");
            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Đơn hàng #{order.Id} đang trên đường giao đến bạn!", $"/Account/OrderDetails/{order.Id}");
            }

            await SendStatusEmailAsync(order, "Đang giao hàng");

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
            
            // 🔥 TÍCH ĐIỂM THƯỞNG (1000đ = 1 điểm)
            if (!string.IsNullOrEmpty(order.UserId))
            {
                var user = await _userManager.FindByIdAsync(order.UserId);
                if (user != null)
                {
                    int pointsEarned = (int)(order.TotalAmount / 1000);
                    if (pointsEarned > 0)
                    {
                        user.RewardPoints += pointsEarned;
                        _context.RewardPointHistories.Add(new RewardPointHistory
                        {
                            UserId = user.Id,
                            PointsChanged = pointsEarned,
                            BalanceAfter = user.RewardPoints,
                            Note = $"Tích điểm từ đơn hàng #{order.Id}",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Đơn hàng #{order.Id} đã giao thành công!", $"/Account/OrderDetails/{order.Id}");
            }

            await SendStatusEmailAsync(order, "Đã giao thành công");

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

            // 🔥 HOÀN TỒN KHO NẾU ĐƠN ĐÃ ĐƯỢC DUYỆT/GIAO TRƯỚC ĐÓ
            if (order.Status == "Confirmed" || order.Status == "Shipping" || order.Status == "Paid")
            {
                foreach (var item in order.Items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                        _context.StockHistories.Add(new StockHistory
                        {
                            ProductId = product.Id,
                            QuantityChange = item.Quantity,
                            BalanceAfter = product.Stock,
                            Type = "Nhập kho (Hủy đơn)",
                            Note = $"Hủy đơn hàng #{order.Id}",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            order.Status = "Cancelled";
            order.CancelReason = cancelReason;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Đơn hàng #{order.Id} đã bị hủy. Lý do: {cancelReason}", $"/Account/OrderDetails/{order.Id}");
            }

            await SendStatusEmailAsync(order, "Đã bị hủy");

            TempData["Success"] = "Đã hủy đơn!";
            return RedirectToAction("Details", new { id });
        }

        // ================= DUYỆT TRẢ HÀNG / HOÀN TIỀN =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveReturn(int id, string adminNote)
        {
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.Status != OrderStatus.ReturnRequested)
            {
                TempData["Error"] = "Đơn hàng không ở trạng thái yêu cầu trả hàng!";
                return RedirectToAction("Details", new { id });
            }

            // 1. Cập nhật trạng thái
            if (order.IsPaid && string.Equals(order.PaymentMethod, "bank", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = OrderStatus.Refunded;
                order.IsPaid = false;
            }
            else
            {
                order.Status = OrderStatus.Returned;
            }
            order.ReturnAdminNote = adminNote;

            // 2. 🔥 HOÀN TỒN KHO
            foreach (var item in order.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                    _context.StockHistories.Add(new StockHistory
                    {
                        ProductId = product.Id,
                        QuantityChange = item.Quantity,
                        BalanceAfter = product.Stock,
                        Type = "Nhập kho (Trả hàng được duyệt)",
                        Note = $"Trả đơn hàng #{order.Id}",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            // 3. 🔥 TRỪ ĐIỂM THƯỞNG ĐÃ NHẬN
            if (!string.IsNullOrEmpty(order.UserId))
            {
                var user = await _userManager.FindByIdAsync(order.UserId);
                if (user != null)
                {
                    int pointsToDeduct = (int)(order.TotalAmount / 1000);
                    if (pointsToDeduct > 0)
                    {
                        user.RewardPoints -= pointsToDeduct;
                        _context.RewardPointHistories.Add(new RewardPointHistory
                        {
                            UserId = user.Id,
                            PointsChanged = -pointsToDeduct,
                            BalanceAfter = user.RewardPoints,
                            Note = $"Thu hồi điểm thưởng do trả hàng (Đơn #{order.Id})",
                            CreatedAt = DateTime.Now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Yêu cầu trả hàng cho đơn #{order.Id} đã được DUYỆT.", $"/Account/OrderDetails/{order.Id}");
            }

            TempData["Success"] = "Đã duyệt trả hàng và hoàn tất quy trình!";
            return RedirectToAction("Details", new { id });
        }

        // ================= TỪ CHỐI TRẢ HÀNG =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReturn(int id, string adminNote)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status != OrderStatus.ReturnRequested)
            {
                TempData["Error"] = "Không có yêu cầu trả hàng để từ chối!";
                return RedirectToAction("Details", new { id });
            }

            if (string.IsNullOrWhiteSpace(adminNote))
            {
                TempData["Error"] = "Vui lòng nhập lý do từ chối!";
                return RedirectToAction("Details", new { id });
            }

            order.Status = OrderStatus.Delivered; // Trả về trạng thái đã giao
            order.ReturnAdminNote = adminNote;

            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(order.UserId))
            {
                await _hubContext.Clients.User(order.UserId).SendAsync("ReceiveUserNotification", $"Yêu cầu trả hàng cho đơn #{order.Id} đã bị TỪ CHỐI. Lý do: {adminNote}", $"/Account/OrderDetails/{order.Id}");
            }

            TempData["Success"] = "Đã từ chối yêu cầu trả hàng.";
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

            await SendStatusEmailAsync(order, newStatus);
            TempData["Success"] = $"Đã cập nhật trạng thái từ {oldStatus} sang {newStatus}!";
            return RedirectToAction("Details", new { id });
        // ================= HELPER =================
        private async Task SendStatusEmailAsync(Order order, string statusDescription)
        {
            if (string.IsNullOrEmpty(order.Email)) return;

            string statusColor = statusDescription switch
            {
                "Đã xác nhận" => "#28a745",
                "Đang giao hàng" => "#17a2b8",
                "Đã giao thành công" => "#28a745",
                "Đã bị hủy" => "#dc3545",
                _ => "#6c757d"
            };

            string emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #eee; padding: 20px; border-radius: 10px;'>
                    <div style='text-align: center; margin-bottom: 20px;'>
                        <h1 style='color: #6f4e37; margin: 0;'>Aura Coffee</h1>
                        <p style='color: #888; margin: 0;'>Hương vị nồng nàn từ cao nguyên</p>
                    </div>
                    <h2 style='color: #333; text-align: center;'>Cập nhật trạng thái đơn hàng</h2>
                    <p>Chào <strong>{order.CustomerName}</strong>,</p>
                    <p>Chúng tôi xin thông báo đơn hàng <strong>#{order.Id}</strong> của bạn đã chuyển sang trạng thái:</p>
                    <div style='background-color: {statusColor}; color: white; padding: 15px 20px; text-align: center; border-radius: 8px; font-weight: bold; margin: 25px 0; font-size: 18px;'>
                        {statusDescription}
                    </div>
                    {(!string.IsNullOrEmpty(order.CancelReason) ? $"<p style='background: #fff5f5; border-left: 4px solid #fc8181; padding: 10px; color: #c53030;'><strong>Lý do:</strong> {order.CancelReason}</p>" : "")}
                    <p>Bạn có thể theo dõi chi tiết đơn hàng tại trang cá nhân hoặc nhấn vào liên kết dưới đây:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='https://localhost:7196/Account/OrderDetails/{order.Id}' style='background: #6f4e37; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Xem chi tiết đơn hàng</a>
                    </div>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;' />
                    <div style='text-align: center; font-size: 12px; color: #888;'>
                        <p>Đây là email tự động, vui lòng không phản hồi email này.</p>
                        <p>© 2024 Aura Coffee. All rights reserved.</p>
                    </div>
                </div>
            ";

            try
            {
                await _emailService.SendEmailAsync(order.Email, $"[Aura Coffee] Cập nhật đơn hàng #{order.Id}", emailBody);
            }
            catch { /* Ignore failures */ }
        }
    }
}