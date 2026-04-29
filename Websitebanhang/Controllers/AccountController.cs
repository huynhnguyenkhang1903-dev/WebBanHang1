using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Models;
using Websitebanhang.Models.ViewModels;
using Websitebanhang.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Websitebanhang.Helpers;
using System.Collections.Generic;

namespace Websitebanhang.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly Websitebanhang.Data.AppDbContext _context;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IWebHostEnvironment env,
            Websitebanhang.Data.AppDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
            _env = env;
            _context = context;
        }

        public IActionResult Login()
        {
            return View();
        }

        private async Task MergeSessionWishlistToDatabaseAsync(ApplicationUser user)
        {
            var sessionWishlist = HttpContext.Session.GetObject<List<CartItem>>("Wishlist");
            if (sessionWishlist != null && sessionWishlist.Any())
            {
                var dbWishlist = await _context.WishlistItems
                    .Where(w => w.UserId == user.Id)
                    .Select(w => w.ProductId)
                    .ToListAsync();

                foreach (var item in sessionWishlist)
                {
                    if (!dbWishlist.Contains(item.ProductId))
                    {
                        _context.WishlistItems.Add(new WishlistItem
                        {
                            UserId = user.Id,
                            ProductId = item.ProductId,
                            AddedAt = System.DateTime.Now
                        });
                    }
                }
                await _context.SaveChangesAsync();
                HttpContext.Session.Remove("Wishlist");
            }
        }

        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // ✅ FIX: CHỈ CÒN 1 OrderDetails
        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (!isAdmin &&
                order.UserId != user.Id &&
                order.Email != user.Email)
            {
                TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("Profile");
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var result = await _signInManager.PasswordSignInAsync(
                        user.UserName!,
                        model.Password,
                        model.RememberMe,
                        lockoutOnFailure: true); // Bật tính năng khóa tài khoản

                    if (result.Succeeded)
                    {
                        await MergeSessionWishlistToDatabaseAsync(user);

                        var roles = await _userManager.GetRolesAsync(user);

                        if (roles.Contains("Admin"))
                            return RedirectToAction("Index", "Admin");

                        return RedirectToAction("Index", "Home");
                    }

                    if (result.IsLockedOut)
                    {
                        ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa tạm thời do nhập sai quá nhiều lần. Vui lòng thử lại sau 5 phút.");
                        return View(model);
                    }
                }

                ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address,
                    DateOfBirth = model.DateOfBirth,
                    EmailConfirmed = true, // Tự động xác thực
                    ReferralCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
                };

                // Xử lý mã giới thiệu nếu có
                if (!string.IsNullOrEmpty(model.ReferralCode))
                {
                    var referrer = await _userManager.Users.FirstOrDefaultAsync(u => u.ReferralCode == model.ReferralCode.ToUpper());
                    if (referrer != null)
                    {
                        user.ReferredByUserId = referrer.Id;
                        
                        // Tặng điểm cho người giới thiệu (50 điểm)
                        referrer.RewardPoints += 50;
                        _context.RewardPointHistories.Add(new RewardPointHistory
                        {
                            UserId = referrer.Id,
                            PointsChanged = 50,
                            BalanceAfter = referrer.RewardPoints,
                            Note = $"Thưởng giới thiệu thành viên mới ({model.Email})",
                            CreatedAt = DateTime.Now
                        });

                        // Tặng điểm cho người được giới thiệu (50 điểm)
                        user.RewardPoints += 50;
                        // Lưu ý: Lịch sử điểm của người mới sẽ được thêm sau khi CreateAsync thành công
                    }
                }

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (user.RewardPoints > 0)
                    {
                        _context.RewardPointHistories.Add(new RewardPointHistory
                        {
                            UserId = user.Id,
                            PointsChanged = 50,
                            BalanceAfter = user.RewardPoints,
                            Note = "Thưởng nhập mã giới thiệu khi đăng ký",
                            CreatedAt = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                    }
                    await _userManager.AddToRoleAsync(user, "User");
                    TempData["SuccessMessage"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi từ nhà cung cấp ngoài: {remoteError}");
                return View("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Thử đăng nhập với tài khoản liên kết
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null)
                {
                    await MergeSessionWishlistToDatabaseAsync(user);

                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin"))
                        return RedirectToAction("Index", "Admin");
                }
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return View("Lockout");
            }
            else
            {
                // Nếu chưa có tài khoản, tự tạo mới
                var email = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Email);
                var name = info.Principal.FindFirstValue(System.Security.Claims.ClaimTypes.Name);

                if (email != null)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = name ?? string.Empty
                        };
                        var createResult = await _userManager.CreateAsync(user);
                        if (createResult.Succeeded)
                        {
                            await _userManager.AddToRoleAsync(user, "User");
                        }
                        else
                        {
                            foreach (var error in createResult.Errors)
                                ModelState.AddModelError(string.Empty, error.Description);
                            return View("Login");
                        }
                    }

                    var linkResult = await _userManager.AddLoginAsync(user, info);
                    if (linkResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        await MergeSessionWishlistToDatabaseAsync(user);
                        return LocalRedirect(returnUrl);
                    }
                }

                ModelState.AddModelError(string.Empty, "Không thể xác thực bằng tài khoản mạng xã hội.");
                return View("Login");
            }
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Email == user.Email)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == user.Id)
                .ToListAsync();

            var viewHistory = await _context.ProductViewHistories
                .Include(h => h.Product)
                .Where(h => h.UserId == user.Id)
                .OrderByDescending(h => h.ViewedAt)
                .Take(10)
                .ToListAsync();

            var pointHistory = await _context.RewardPointHistories
                .Where(h => h.UserId == user.Id)
                .OrderByDescending(h => h.CreatedAt)
                .Take(20)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Orders = orders,
                Addresses = addresses,
                ViewHistory = viewHistory,
                RewardPoints = user.RewardPoints,
                ReferralCode = user.ReferralCode,
                PointHistory = pointHistory
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            user.FullName = model.FullName ?? "";
            user.Address = model.Address ?? "";
            user.PhoneNumber = model.PhoneNumber;
            user.DateOfBirth = model.DateOfBirth;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cập nhật thành công!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.CurrentPassword,
                model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id, string cancelReason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.Email == user.Email);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn!";
                return RedirectToAction("Profile");
            }

            if (order.Status != "Pending")
            {
                TempData["Error"] = "Chỉ hủy đơn đang chờ!";
                return RedirectToAction("Profile");
            }

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                TempData["Error"] = "Vui lòng nhập lý do hủy đơn!";
                return RedirectToAction("CancelOrder", new { id });
            }

            // If order was paid by bank, perform refund
            if (order.IsPaid && string.Equals(order.PaymentMethod, "bank", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = Models.OrderStatus.Refunded;
                order.IsPaid = false;
                order.TransactionId = null;
                order.CancelReason = cancelReason;
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã hủy đơn và hoàn tiền (chuyển khoản).";
                return RedirectToAction("Profile");
            }

            order.Status = "Cancelled";
            // ✅ LUÔN gán trước
            order.CancelReason = cancelReason;

            // If order was paid by bank, perform refund
            if (order.IsPaid && string.Equals(order.PaymentMethod, "bank", StringComparison.OrdinalIgnoreCase))
            {
                order.Status = Models.OrderStatus.Refunded;
                order.IsPaid = false;
                order.TransactionId = null;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã hủy đơn và hoàn tiền (chuyển khoản).";
                return RedirectToAction("Profile");
            }

            order.Status = "Cancelled";

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã hủy đơn!";
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.Email == user.Email);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn!";
                return RedirectToAction("Profile");
            }

            if (order.Status != "Pending")
            {
                TempData["Error"] = "Chỉ hủy đơn đang chờ!";
                return RedirectToAction("Profile");
            }

            return View(order);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReturn(int id, string reasonType, string returnReason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.Email == user.Email);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn!";
                return RedirectToAction("Profile");
            }

            if (order.Status != OrderStatus.Delivered)
            {
                TempData["Error"] = "Chỉ có thể yêu cầu trả hàng cho đơn đã giao thành công.";
                return RedirectToAction("OrderDetails", new { id });
            }

            var fullReason = reasonType;
            if (reasonType == "Lý do khác" && !string.IsNullOrWhiteSpace(returnReason))
            {
                fullReason = returnReason;
            }
            else if (!string.IsNullOrWhiteSpace(returnReason))
            {
                fullReason = $"{reasonType}: {returnReason}";
            }

            order.Status = OrderStatus.ReturnRequested;
            order.ReturnReason = fullReason;
            order.ReturnRequestedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Yêu cầu trả hàng đã được gửi thành công. Vui lòng chờ Admin xét duyệt.";
            return RedirectToAction("OrderDetails", new { id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceived(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.Email == user.Email);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Profile");
            }

            if (order.Status != OrderStatus.Shipping)
            {
                TempData["Error"] = "Đơn hàng phải ở trạng thái đang giao mới có thể xác nhận!";
                return RedirectToAction("OrderDetails", new { id });
            }

            order.Status = OrderStatus.Delivered;

            // 🔥 TÍCH ĐIỂM THƯỞNG (1000đ = 1 điểm)
            int pointsEarned = (int)(order.TotalAmount / 1000);
            if (pointsEarned > 0)
            {
                user.RewardPoints += pointsEarned;
                _context.RewardPointHistories.Add(new RewardPointHistory
                {
                    UserId = user.Id,
                    PointsChanged = pointsEarned,
                    BalanceAfter = user.RewardPoints,
                    Note = $"Tích điểm từ đơn hàng #{order.Id} (Người dùng xác nhận)",
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xác nhận đã nhận hàng thành công! Bạn đã được cộng điểm thưởng.";
            return RedirectToAction("OrderDetails", new { id });
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        [Authorize]
        [HttpGet]
        public IActionResult UploadAvatar()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (avatarFile == null || avatarFile.Length == 0)
            {
                ModelState.AddModelError("", "Vui lòng chọn ảnh!");
                ViewBag.CurrentAvatar = user.AvatarUrl;
                return View();
            }

            // Giới hạn 2MB
            if (avatarFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError("", "Ảnh vượt quá 2MB!");
                ViewBag.CurrentAvatar = user.AvatarUrl;
                return View();
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(avatarFile.FileName);
            var uploadPath = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }

            user.AvatarUrl = "/uploads/" + fileName;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "Upload thành công!";
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            if (string.IsNullOrEmpty(user.AvatarUrl))
            {
                TempData["Error"] = "Không có ảnh đại diện để xóa.";
                return RedirectToAction("Profile");
            }

            // Only attempt to delete local files under wwwroot
            try
            {
                var avatar = user.AvatarUrl ?? string.Empty;
                if (!avatar.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    // remove leading slash if present
                    var relativePath = avatar.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString());
                    var fullPath = Path.Combine(_env.WebRootPath ?? string.Empty, relativePath);

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }
            catch
            {
                // ignore file delete errors
            }

            user.AvatarUrl = string.Empty;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Đã xóa ảnh đại diện.";
                // refresh sign-in so claims updated if avatar used in claims
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction("Profile");
            }

            TempData["Error"] = "Không thể xóa ảnh đại diện.";
            return RedirectToAction("Profile");
        }

        // ================= IN HÓA ĐƠN =================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.Email == user.Email);

            if (order == null)
                return NotFound();

            return View("PrintInvoice", order);
        }

        // ================= ADDRESS BOOK =================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(UserAddress model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            model.UserId = user.Id;

            var existingAddressesCount = await _context.UserAddresses.CountAsync(a => a.UserId == user.Id);
            if (existingAddressesCount == 0 || model.IsDefault)
            {
                if (model.IsDefault)
                {
                    var oldDefaults = await _context.UserAddresses.Where(a => a.UserId == user.Id && a.IsDefault).ToListAsync();
                    foreach (var addr in oldDefaults) addr.IsDefault = false;
                }
                else if (existingAddressesCount == 0)
                {
                    model.IsDefault = true;
                }
            }

            _context.UserAddresses.Add(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã thêm địa chỉ mới!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(int id, UserAddress model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var addr = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (addr == null) return NotFound();

            addr.FullName = model.FullName;
            addr.PhoneNumber = model.PhoneNumber;
            addr.AddressLine = model.AddressLine;

            if (model.IsDefault && !addr.IsDefault)
            {
                var oldDefaults = await _context.UserAddresses.Where(a => a.UserId == user.Id && a.IsDefault).ToListAsync();
                foreach (var old in oldDefaults) old.IsDefault = false;
                addr.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật địa chỉ!";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var addr = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (addr != null)
            {
                _context.UserAddresses.Remove(addr);
                await _context.SaveChangesAsync();

                if (addr.IsDefault)
                {
                    var firstRemaining = await _context.UserAddresses.FirstOrDefaultAsync(a => a.UserId == user.Id);
                    if (firstRemaining != null)
                    {
                        firstRemaining.IsDefault = true;
                        await _context.SaveChangesAsync();
                    }
                }
                TempData["SuccessMessage"] = "Đã xóa địa chỉ!";
            }
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultAddress(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var oldDefaults = await _context.UserAddresses.Where(a => a.UserId == user.Id && a.IsDefault).ToListAsync();
            foreach (var old in oldDefaults) old.IsDefault = false;

            var addr = await _context.UserAddresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == user.Id);
            if (addr != null)
            {
                addr.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã đặt làm địa chỉ mặc định!";
            return RedirectToAction("Profile");
        }
    }
}