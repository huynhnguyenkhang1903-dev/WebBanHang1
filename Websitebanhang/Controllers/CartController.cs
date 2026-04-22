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

namespace Websitebanhang.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly IEmailService _emailService;
        private readonly Websitebanhang.Data.AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(
            IProductRepository productRepository,
            IEmailService emailService,
            Websitebanhang.Data.AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _productRepository = productRepository;
            _emailService = emailService;
            _context = context;
            _userManager = userManager;
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

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
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
            var vouchers = _context.Voucher
                .Where(v => v.ExpiryDate >= DateTime.Now)
                .OrderBy(v => v.ExpiryDate)
                .ToList();

            ViewBag.Vouchers = vouchers;
            ViewBag.ShippingVouchers = vouchers; // reuse same list for shipping vouchers

            return View(selectedItems);
        }

        // ================= PLACE ORDER (GET) =================
        [Authorize]
        [HttpGet]
        public IActionResult PlaceOrder()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (cart.Count == 0)
            {
                TempData["Error"] = "Giỏ hàng trống.";
                return RedirectToAction("Index");
            }

            return View(cart);
        }

        // ================= ĐẶT HÀNG =================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PlaceOrder(string name, string email, string address, string phone, string paymentMethod, string shippingMethod, string? voucherCode, string? shippingVoucherCode)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (cart.Count == 0) return RedirectToAction("Index");

            decimal subtotal = cart.Sum(p => p.Price * p.Quantity);

            // Build cart debug html
            var sb = new StringBuilder();
            sb.AppendLine("<table class=\"table\"><thead><tr><th>Tên</th><th>Giá</th><th>SL</th><th>Tổng</th></tr></thead><tbody>");
            foreach (var it in cart)
            {
                sb.AppendLine($"<tr><td>{System.Net.WebUtility.HtmlEncode(it.Name)}</td><td>{it.Price.ToString("N0")} ₫</td><td>{it.Quantity}</td><td>{(it.Price * it.Quantity).ToString("N0")} ₫</td></tr>");
            }
            sb.AppendLine($"</tbody><tfoot><tr><th colspan=3>Tạm tính</th><th>{subtotal.ToString("N0")} ₫</th></tr></tfoot></table>");
            TempData["Debug_CartHtml"] = sb.ToString();

            // Determine shipping cost server-side (must match client JS)
            decimal shippingCost = 0m;
            if (string.Equals(shippingMethod, "fast", StringComparison.OrdinalIgnoreCase))
                shippingCost = 30000m;
            else
                shippingCost = 10000m; // economy default

            // Apply order voucher discount if provided
            Voucher? appliedVoucher = null;
            int orderVoucherPercent = 0;
            if (!string.IsNullOrEmpty(voucherCode))
            {
                appliedVoucher = _context.Voucher.FirstOrDefault(v => v.Code == voucherCode && v.ExpiryDate >= DateTime.Now);
                if (appliedVoucher != null)
                    orderVoucherPercent = appliedVoucher.DiscountPercent;
            }

            decimal orderDiscountAmount = Math.Round(subtotal * (orderVoucherPercent / 100m));

            // Apply shipping voucher if provided
            Voucher? appliedShippingVoucher = null;
            int shippingVoucherPercent = 0;
            if (!string.IsNullOrEmpty(shippingVoucherCode))
            {
                appliedShippingVoucher = _context.Voucher.FirstOrDefault(v => v.Code == shippingVoucherCode && v.ExpiryDate >= DateTime.Now);
                if (appliedShippingVoucher != null)
                    shippingVoucherPercent = appliedShippingVoucher.DiscountPercent;
            }

            decimal shippingDiscountAmount = Math.Round(shippingCost * (shippingVoucherPercent / 100m));
            decimal shippingFinal = Math.Max(0m, shippingCost - shippingDiscountAmount);

            decimal finalTotal = Math.Max(0m, subtotal - orderDiscountAmount + shippingFinal);

            // TEMP DEBUG: store values to TempData for inspection in view
            TempData["Debug_Subtotal"] = subtotal.ToString("N0");
            TempData["Debug_OrderDiscount"] = orderDiscountAmount.ToString("N0");
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
                OrderDate = DateTime.Now,
                Status = "Pending",
                IsPaid = false,
                Items = cart.Select(c => new CartItem
                {
                    ProductId = c.ProductId,
                    Name = c.Name,
                    Price = c.Price,
                    Quantity = c.Quantity,
                    ImageUrl = c.ImageUrl
                }).ToList()
            };

            // If user is authenticated, set UserId and prefer account email
            try
            {
                var appUser = await _userManager.GetUserAsync(User);
                if (appUser != null)
                {
                    order.UserId = appUser.Id;
                    // prefer user's email from account if form email is empty or different
                    if (string.IsNullOrEmpty(order.Email) || !string.Equals(order.Email, appUser.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        order.Email = appUser.Email;
                    }
                }
            }
            catch
            {
                // ignore if userManager not available for any reason
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            order.PaymentContent = $"ORDER{order.Id}";

            // persist applied voucher info on order
            if (appliedVoucher != null)
            {
                order.VoucherCode = appliedVoucher.Code;
                order.VoucherDiscountPercent = appliedVoucher.DiscountPercent;
                order.VoucherExpires = appliedVoucher.ExpiryDate;
            }

            if (appliedShippingVoucher != null)
            {
                order.ShippingVoucherCode = appliedShippingVoucher.Code;
                order.ShippingVoucherDiscountPercent = appliedShippingVoucher.DiscountPercent;
            }

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
    }
}