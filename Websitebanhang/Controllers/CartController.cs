using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Models;
using Websitebanhang.Repositores;
using Websitebanhang.Helpers;
using Websitebanhang.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Websitebanhang.Hubs;

namespace Websitebanhang.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly IEmailService _emailService;
        private readonly Websitebanhang.Data.AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IWebsiteSettingService _settingService;

        public CartController(
            IProductRepository productRepository,
            IEmailService emailService,
            Websitebanhang.Data.AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> hubContext,
            IWebsiteSettingService settingService)
        {
            _productRepository = productRepository;
            _emailService = emailService;
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _settingService = settingService;
        }

        // ================= GIỎ HÀNG =================
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            return View(cart);
        }

        public IActionResult AddToCart(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            if (product.Stock <= 0)
            {
                TempData["Error"] = "Sản phẩm này đã hết hàng!";
                return RedirectToAction("Index");
            }

            if (product.ExpiryDate.HasValue && product.ExpiryDate.Value <= DateTime.Now)
            {
                TempData["Error"] = "Sản phẩm này đã hết hạn sử dụng, không thể mua!";
                return RedirectToAction("Index");
            }

            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item != null)
                item.Quantity++;
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name ?? "",
                    Price = product.Price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl ?? ""
                });
            }

            HttpContext.Session.SetObject("Cart", cart);
            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item != null)
                cart.Remove(item);

            HttpContext.Session.SetObject("Cart", cart);
            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            HttpContext.Session.Remove("Cart");
            TempData["Success"] = "Đã làm trống giỏ hàng!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var product = _productRepository.GetById(productId);
            if (product == null) return NotFound();

            if (quantity > product.Stock)
            {
                TempData["Error"] = $"Xin lỗi, sản phẩm này chỉ còn {product.Stock} trong kho!";
                return RedirectToAction("Index");
            }

            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(p => p.ProductId == productId);

            if (item != null)
                item.Quantity = quantity;

            HttpContext.Session.SetObject("Cart", cart);
            return RedirectToAction("Index");
        }

        // ================= CHECKOUT =================
        [HttpPost]
        public IActionResult Checkout(List<int> selectedProducts)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

            var selectedItems = cart
                .Where(p => selectedProducts.Contains(p.ProductId))
                .ToList();

            // Save selected items into session so PlaceOrder uses the same selection
            HttpContext.Session.SetObject("Cart", selectedItems);

            // load available vouchers
            var vouchersList = _context.Voucher
                .Where(v => v.ExpiryDate >= DateTime.Now)
                .Select(v => new { Code = v.Code, DiscountPercent = v.DiscountPercent, ExpiryDate = v.ExpiryDate, Type = "Voucher" })
                .ToList();

            // load active promotions with codes
            var promotionsList = _context.Promotions
                .Where(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
                .Select(p => new { Code = p.Code, DiscountPercent = p.DiscountPercent, ExpiryDate = p.EndDate, Type = "Promotion" })
                .ToList();

            // Merge both
            var allVouchers = vouchersList.Concat(promotionsList)
                .OrderBy(v => v.ExpiryDate)
                .ToList();

            ViewBag.Vouchers = allVouchers;
            ViewBag.ShippingVouchers = allVouchers; 

            return View(selectedItems);
        }

        // ================= PLACE ORDER (GET) =================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PlaceOrder()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng trống.";
                return RedirectToAction("Index");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                ViewBag.UserAddresses = await _context.UserAddresses
                    .Where(a => a.UserId == user.Id)
                    .OrderByDescending(a => a.IsDefault)
                    .ToListAsync();
            }

            return View(cart);
        }

        // ================= ĐẶT HÀNG =================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PlaceOrder(string name, string email, string address, string phone, string paymentMethod, string shippingProvider, string? orderNotes, string? voucherCode, string? shippingVoucherCode, bool usePoints = false)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (cart.Count == 0) return RedirectToAction("Index");

            decimal subtotal = cart.Sum(p => p.Price * p.Quantity);
            var appUser = await _userManager.GetUserAsync(User);
            if (appUser == null) return RedirectToAction("Login", "Account");

            // Apply order voucher discount if provided
            int orderVoucherPercent = 0;
            string? finalVoucherCode = null;
            if (!string.IsNullOrEmpty(voucherCode))
            {
                var v = await _context.Voucher.FirstOrDefaultAsync(v => v.Code == voucherCode && v.ExpiryDate >= DateTime.Now);
                if (v != null)
                {
                    orderVoucherPercent = v.DiscountPercent;
                    finalVoucherCode = v.Code;
                }
                else
                {
                    var p = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == voucherCode && p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now);
                    if (p != null)
                    {
                        orderVoucherPercent = p.DiscountPercent;
                        finalVoucherCode = p.Code;
                    }
                }
            }

            decimal orderDiscountAmount = Math.Round(subtotal * (orderVoucherPercent / 100m));

            // Determine shipping cost server-side
            decimal shippingCost = 0m;
            if (shippingProvider == "Viettel Post") shippingCost = 35000m;
            else if (shippingProvider == "Giao Hàng Nhanh") shippingCost = 30000m;
            else if (shippingProvider == "Nhận tại cửa hàng") shippingCost = 0m;
            else shippingCost = 15000m; // Default backup

            // Apply shipping voucher if provided
            int shippingVoucherPercent = 0;
            string? finalShippingVoucherCode = null;
            if (!string.IsNullOrEmpty(shippingVoucherCode))
            {
                var sv = await _context.Voucher.FirstOrDefaultAsync(v => v.Code == shippingVoucherCode && v.ExpiryDate >= DateTime.Now);
                if (sv != null)
                {
                    shippingVoucherPercent = sv.DiscountPercent;
                    finalShippingVoucherCode = sv.Code;
                }
                else
                {
                    var sp = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == shippingVoucherCode && p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now);
                    if (sp != null)
                    {
                        shippingVoucherPercent = sp.DiscountPercent;
                        finalShippingVoucherCode = sp.Code;
                    }
                }
            }

            decimal shippingDiscountAmount = Math.Round(shippingCost * (shippingVoucherPercent / 100m));
            decimal shippingFinal = Math.Max(0m, shippingCost - shippingDiscountAmount);

            // 🔥 XỬ LÝ ĐIỂM THƯỞNG
            decimal pointsDiscount = 0;
            int pointsToUse = 0;
            if (usePoints && appUser.RewardPoints > 0)
            {
                // 1 điểm = 100đ
                pointsDiscount = appUser.RewardPoints * 100;
                
                // Không giảm quá tổng tiền (trừ đi các voucher)
                decimal maxAllowedDiscount = subtotal - orderDiscountAmount;
                if (pointsDiscount > maxAllowedDiscount)
                {
                    pointsDiscount = maxAllowedDiscount;
                    pointsToUse = (int)Math.Ceiling(pointsDiscount / 100);
                }
                else
                {
                    pointsToUse = appUser.RewardPoints;
                }
            }

            decimal finalTotal = Math.Max(0m, subtotal - orderDiscountAmount - pointsDiscount + shippingFinal);

            // Deduct points
            if (pointsToUse > 0)
            {
                appUser.RewardPoints -= pointsToUse;
                _context.RewardPointHistories.Add(new RewardPointHistory
                {
                    UserId = appUser.Id,
                    PointsChanged = -pointsToUse,
                    BalanceAfter = appUser.RewardPoints,
                    Note = $"Sử dụng điểm cho đơn hàng mới",
                    CreatedAt = DateTime.Now
                });
            }

            // TEMP DEBUG: store values to TempData for inspection in view
            TempData["Debug_Subtotal"] = subtotal.ToString("N0");
            TempData["Debug_OrderDiscount"] = orderDiscountAmount.ToString("N0");
            TempData["Debug_PointsDiscount"] = pointsDiscount.ToString("N0");
            TempData["Debug_ShippingCost"] = shippingCost.ToString("N0");
            TempData["Debug_ShippingDiscount"] = shippingDiscountAmount.ToString("N0");
            TempData["Debug_FinalTotal"] = finalTotal.ToString("N0");

            var order = new Order
            {
                CustomerName = name,
                Email = email,
                Address = address,
                Phone = phone,
                PaymentMethod = paymentMethod,
                TotalAmount = finalTotal,
                ShippingProvider = shippingProvider ?? "GHTK",
                ShippingCost = shippingCost,
                OrderDate = DateTime.Now,
                Status = "Pending",
                OrderNotes = orderNotes,
                IsPaid = false,
                Items = cart.Select(c => new CartItem
                {
                    ProductId = c.ProductId,
                    Name = c.Name,
                    Price = c.Price,
                    Quantity = c.Quantity,
                    ImageUrl = c.ImageUrl
                }).ToList(),
                VoucherCode = finalVoucherCode,
                VoucherDiscountPercent = orderVoucherPercent,
                ShippingVoucherCode = finalShippingVoucherCode,
                ShippingVoucherDiscountPercent = shippingVoucherPercent
            };

            // If user is authenticated, set UserId and prefer account email
            if (appUser != null)
            {
                order.UserId = appUser.Id;
                // prefer user's email from account if form email is empty or different
                if (string.IsNullOrEmpty(order.Email) || !string.Equals(order.Email, appUser.Email, StringComparison.OrdinalIgnoreCase))
                {
                    order.Email = appUser.Email ?? "";
                }
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 🔥 Bắn thông báo realtime cho admin
            await _hubContext.Clients.All.SendAsync("ReceiveAdminNotification", $"Có đơn hàng mới #{order.Id} từ {order.CustomerName}!", $"/AdminOrder/Details/{order.Id}");

            order.PaymentContent = $"ORDER{order.Id}";
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // If DB schema not updated to include voucher columns, ignore and continue
                TempData["Warning"] = "Không thể lưu mã voucher vào cơ sở dữ liệu (cần chạy migration). Đơn hàng đã được tạo.";
            }

            HttpContext.Session.Remove("Cart");

            // --- SEND CONFIRMATION EMAIL (PREMIUM TEMPLATE) ---
            try
            {
                var siteTitle = await _settingService.GetSettingAsync("SiteTitle", "Aura Coffee");
                string paymentText = (order.PaymentMethod == "bank" ? "Chuyển khoản ngân hàng" : "Thanh toán khi nhận hàng (COD)");
                
                string emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #eee; padding: 20px; border-radius: 10px;'>
                        <div style='text-align: center; margin-bottom: 20px;'>
                            <h1 style='color: #6f4e37; margin: 0;'>{siteTitle}</h1>
                            <p style='color: #888; margin: 0;'>Cảm ơn bạn đã tin tưởng chúng tôi</p>
                        </div>
                        <div style='background-color: #fdfaf7; padding: 20px; border-radius: 8px; margin-bottom: 25px;'>
                            <h2 style='color: #6f4e37; text-align: center; margin-top: 0;'>Đặt hàng thành công!</h2>
                            <p>Chào <strong>{order.CustomerName}</strong>,</p>
                            <p>Đơn hàng <strong>#{order.Id}</strong> của bạn đã được tiếp nhận và đang chờ xử lý.</p>
                        </div>
                        <h3 style='color: #333; border-bottom: 2px solid #6f4e37; padding-bottom: 5px;'>Chi tiết đơn hàng:</h3>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Tổng tiền:</td>
                                <td style='padding: 8px 0; text-align: right; font-weight: bold; color: #d33;'>{order.TotalAmount:N0} ₫</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Thanh toán:</td>
                                <td style='padding: 8px 0; text-align: right;'>{paymentText}</td>
                            </tr>
                            <tr>
                                <td style='padding: 8px 0; color: #666;'>Địa chỉ:</td>
                                <td style='padding: 8px 0; text-align: right;'>{order.Address}</td>
                            </tr>
                        </table>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='https://localhost:7196/Account/OrderDetails/{order.Id}' style='background: #6f4e37; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Xem chi tiết đơn hàng</a>
                        </div>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 30px 0;' />
                        <div style='text-align: center; font-size: 12px; color: #888;'>
                            <p>Đây là email tự động, vui lòng không phản hồi email này.</p>
                            <p>© {DateTime.Now.Year} {siteTitle}. All rights reserved.</p>
                        </div>
                    </div>
                ";
                await _emailService.SendEmailAsync(order.Email, $"[{siteTitle}] Xác nhận đơn hàng #{order.Id}", emailBody);
            }
            catch
            {
                // Ignore email sending error so order process doesn't fail
            }
            // ---------------------------------------------------

            // 🔥 BANK
            if (paymentMethod == "bank")
            {
                string bankCode = "970422";
                string accountNumber = "0344506553";
                string accountName = "HUYNH NGUYEN KHANG";

                string qrUrl = $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact.png" +
                               $"?amount={order.TotalAmount}" +
                               $"&addInfo={order.PaymentContent}" +
                               $"&accountName={accountName}";

                ViewBag.QRCode = qrUrl;
                ViewBag.Amount = order.TotalAmount;
                ViewBag.Content = order.PaymentContent;
                ViewBag.OrderId = order.Id;

                // pass bank details to view
                ViewBag.BankCode = bankCode;
                ViewBag.AccountNumber = accountNumber;
                ViewBag.AccountName = accountName;

                return View("BankPayment");
            }

            return View("OrderSuccess", order);
        }

        // ================= HỦY ĐƠN =================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (order.IsPaid)
            {
                TempData["Error"] = "Đơn đã thanh toán, không thể huỷ!";
                return RedirectToAction("Profile", "Account");
            }

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Huỷ đơn thành công!";
            return RedirectToAction("Profile", "Account");
        }

        // GET: /Cart/Cancel/5
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var appUser = await _userManager.GetUserAsync(User);
            var isAdmin = appUser != null && await _userManager.IsInRoleAsync(appUser, "Admin");

            // Allow admin or owner (by UserId or Email) to view cancel page
            if (!isAdmin)
            {
                if (appUser == null || (!string.Equals(order.UserId, appUser.Id, StringComparison.OrdinalIgnoreCase) && !string.Equals(order.Email, appUser.Email, StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["Error"] = "Bạn không có quyền hủy đơn này.";
                    return RedirectToAction("Index", "Account");
                }
            }

            // Only pending orders can be cancelled by user
            if (order.IsPaid || order.Status == "Delivered")
            {
                TempData["Error"] = "Đơn hàng không thể hủy.";
                return RedirectToAction("Profile", "Account");
            }

            return View(order);
        }

        // ================= CHECK PAYMENT =================
        public async Task<IActionResult> CheckPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            // 🔥 CHẶN nếu đã huỷ
            if (order.Status == "Cancelled")
                return BadRequest("Đơn đã bị huỷ");

            order.IsPaid = true;
            order.Status = "Paid";
            order.PaidAt = DateTime.Now;
            order.TransactionId = Guid.NewGuid().ToString();

            await _context.SaveChangesAsync();

            return RedirectToAction("PaymentSuccess", new { id = orderId });
        }

        // ================= SUCCESS =================
        public IActionResult OrderSuccess()
        {
            return View();
        }

        public IActionResult PaymentSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        // ================= FAILED PAYMENT =================
        [HttpPost]
        public async Task<IActionResult> FailPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (order.IsPaid || order.Status == "Cancelled")
            {
                return BadRequest("Không thể báo lỗi thanh toán cho đơn hàng này.");
            }

            // Could change status to 'Payment Failed' or keep as 'Pending' so they can retry.
            // Let's just keep it as Pending so they can retry or cancel from Profile.
            
            return RedirectToAction("PaymentFailed", new { id = orderId });
        }

        public IActionResult PaymentFailed(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }
        // ================= KIỂM TRA VOUCHER =================
        [HttpGet]
        public IActionResult ValidateVoucher(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return Json(new { success = false, message = "Vui lòng nhập mã!" });

            var voucher = _context.Voucher.FirstOrDefault(v => v.Code == code);

            if (voucher == null)
                return Json(new { success = false, message = "Mã giảm giá không tồn tại!" });

            if (voucher.ExpiryDate < DateTime.Now)
                return Json(new { success = false, message = "Mã giảm giá đã hết hạn!" });

            return Json(new { 
                success = true, 
                message = "Áp dụng mã thành công!", 
                discountPercent = voucher.DiscountPercent 
            });
        }
    }
}