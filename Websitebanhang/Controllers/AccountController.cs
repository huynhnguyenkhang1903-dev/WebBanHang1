using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Models;
using Websitebanhang.Models.ViewModels;
using Websitebanhang.Services;
using System.Threading.Tasks;
using System.Web;
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
        private readonly IWebsiteSettingService _settingService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IWebHostEnvironment env,
            Websitebanhang.Data.AppDbContext context,
            IWebsiteSettingService settingService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
            _env = env;
            _context = context;
            _settingService = settingService;
        }

        public IActionResult Login(string remoteError = null)
        {
            if (!string.IsNullOrEmpty(remoteError))
            {
                ModelState.AddModelError("", remoteError);
            }
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
            var user = await _userManager.GetUserAsync(User!);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User!);
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetOrderStatus(int id)
        {
            var user = await _userManager.GetUserAsync(User!);
            if (user == null) return Unauthorized();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (!isAdmin && order.UserId != user.Id && order.Email != user.Email) return Forbid();

            return Json(new { status = order.Status });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                        lockoutOnFailure: true);

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

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ModelState.AddModelError("", $"Lỗi từ dịch vụ ngoài: {remoteError}");
                return RedirectToAction("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null)
                {
                    await MergeSessionWishlistToDatabaseAsync(user);
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin")) return RedirectToAction("Index", "Admin");
                }
                return LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return RedirectToAction("Lockout");
            }
            else
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var name = info.Principal.FindFirstValue(ClaimTypes.Name);

                if (email != null)
                {
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        user = new ApplicationUser
                        {
                            UserName = email,
                            Email = email,
                            FullName = name ?? email,
                            EmailConfirmed = true,
                            ReferralCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
                        };
                        await _userManager.CreateAsync(user);
                        await _userManager.AddToRoleAsync(user, "User");
                    }

                    await _userManager.AddLoginAsync(user, info);
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    await MergeSessionWishlistToDatabaseAsync(user);
                    return LocalRedirect(returnUrl);
                }

                return RedirectToAction("Login");
            }
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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
                    EmailConfirmed = false,
                    ReferralCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
                };

                if (!string.IsNullOrEmpty(model.ReferralCode))
                {
                    var referrer = await _userManager.Users.FirstOrDefaultAsync(u => u.ReferralCode == model.ReferralCode.ToUpper());
                    if (referrer != null)
                    {
                        user.ReferredByUserId = referrer.Id;
                        referrer.RewardPoints += 50;
                        _context.RewardPointHistories.Add(new RewardPointHistory
                        {
                            UserId = referrer.Id,
                            PointsChanged = 50,
                            BalanceAfter = referrer.RewardPoints,
                            Note = $"Thưởng giới thiệu thành viên mới ({model.Email})",
                            CreatedAt = DateTime.Now
                        });
                        user.RewardPoints += 50;
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
                    
                    var otp = new Random().Next(100000, 999999).ToString();
                    user.OtpCode = otp;
                    user.OtpExpiry = DateTime.Now.AddMinutes(5);
                    await _userManager.UpdateAsync(user);

                    try
                    {
                        await _emailService.SendEmailAsync(user.Email!, "Xác thực tài khoản - Aura Coffee", 
                            $"<div style='font-family: Arial; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>" +
                            $"<h2 style='color: #6f4e37;'>Xác thực tài khoản</h2>" +
                            $"<p>Chào <strong>{user.FullName}</strong>,</p>" +
                            $"<p>Mã xác thực (OTP) của bạn là:</p>" +
                            $"<div style='font-size: 24px; font-weight: bold; color: #6f4e37; letter-spacing: 5px; margin: 20px 0;'>{otp}</div>" +
                            $"<p>Mã có hiệu lực trong 5 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>" +
                            $"</div>");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Email failed: {ex.Message}");
                    }

                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng kiểm tra email.";
                    return RedirectToAction("VerifyEmail", new { email = user.Email });
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(string email, string otp)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound();

            if (user.OtpCode == otp && user.OtpExpiry > DateTime.Now)
            {
                user.EmailConfirmed = true;
                user.OtpCode = null;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = "Xác thực thành công!";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Mã OTP không đúng hoặc hết hạn.");
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResendOtp(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return Json(new { success = false });

            var otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpiry = DateTime.Now.AddMinutes(5);
            await _userManager.UpdateAsync(user);

            try { await _emailService.SendEmailAsync(user.Email!, "Mã OTP mới", $"Mã mới: {otp}"); } catch {}

            return Json(new { success = true });
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User!);
            if (user == null) return RedirectToAction("Login");

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Email == user.Email)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var addresses = await _context.UserAddresses.Where(a => a.UserId == user.Id).ToListAsync();
            var viewHistory = await _context.ProductViewHistories.Include(h => h.Product).Where(h => h.UserId == user.Id).OrderByDescending(h => h.ViewedAt).Take(10).ToListAsync();
            var pointHistory = await _context.RewardPointHistories.Where(h => h.UserId == user.Id).OrderByDescending(h => h.CreatedAt).Take(20).ToListAsync();

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
            var user = await _userManager.GetUserAsync(User!);
            if (user == null) return RedirectToAction("Login");

            if (!ModelState.IsValid) 
            {
                model.Orders = await _context.Orders.Include(o => o.Items).Where(o => o.Email == user.Email).OrderByDescending(o => o.OrderDate).ToListAsync();
                model.Addresses = await _context.UserAddresses.Where(a => a.UserId == user.Id).ToListAsync();
                model.ViewHistory = await _context.ProductViewHistories.Include(h => h.Product).Where(h => h.UserId == user.Id).OrderByDescending(h => h.ViewedAt).Take(10).ToListAsync();
                model.PointHistory = await _context.RewardPointHistories.Where(h => h.UserId == user.Id).OrderByDescending(h => h.CreatedAt).Take(20).ToListAsync();
                model.RewardPoints = user.RewardPoints;
                model.ReferralCode = user.ReferralCode;
                model.Email = user.Email;
                return View(model);
            }

            user!.FullName = model.FullName ?? "";
            user.Address = model.Address ?? "";
            user.PhoneNumber = model.PhoneNumber;
            user.DateOfBirth = model.DateOfBirth;
            
            bool isUserNameChanged = false;
            if (!string.IsNullOrWhiteSpace(model.UserName) && model.UserName != user.UserName)
            {
                user.UserName = model.UserName;
                isUserNameChanged = true;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (isUserNameChanged)
                {
                    await _signInManager.RefreshSignInAsync(user);
                }
                TempData["SuccessMessage"] = "Cập nhật thành công!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            
            model.Orders = await _context.Orders.Include(o => o.Items).Where(o => o.Email == user.Email).OrderByDescending(o => o.OrderDate).ToListAsync();
            model.Addresses = await _context.UserAddresses.Where(a => a.UserId == user.Id).ToListAsync();
            model.ViewHistory = await _context.ProductViewHistories.Include(h => h.Product).Where(h => h.UserId == user.Id).OrderByDescending(h => h.ViewedAt).Take(10).ToListAsync();
            model.PointHistory = await _context.RewardPointHistories.Where(h => h.UserId == user.Id).OrderByDescending(h => h.CreatedAt).Take(20).ToListAsync();
            model.RewardPoints = user.RewardPoints;
            model.ReferralCode = user.ReferralCode;
            model.Email = user.Email;
            
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail(string newEmail)
        {
            var user = await _userManager.GetUserAsync(User!);
            if (user == null) return RedirectToAction("Login");

            user.Email = newEmail;
            user.UserName = newEmail;
            user.EmailConfirmed = false;
            
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                var otp = new Random().Next(100000, 999999).ToString();
                user.OtpCode = otp;
                user.OtpExpiry = DateTime.Now.AddMinutes(5);
                await _userManager.UpdateAsync(user);
                await _signInManager.RefreshSignInAsync(user);
                return RedirectToAction("VerifyEmail", new { email = user.Email });
            }
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhone(string newPhone)
        {
            var user = await _userManager.GetUserAsync(User!);
            if (user == null) return RedirectToAction("Login");
            user.PhoneNumber = newPhone;
            await _userManager.UpdateAsync(user);
            return RedirectToAction("Profile");
        }

        [Authorize]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.GetUserAsync(User!);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id, string cancelReason)
        {
            var user = await _userManager.GetUserAsync(User!);
            if (user == null) return RedirectToAction("Login");
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.Email == user.Email);
            if (order == null || order.Status != "Pending") return RedirectToAction("Profile");

            order.Status = "Cancelled";
            order.CancelReason = cancelReason;
            await _context.SaveChangesAsync();
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User!);
            if (user == null) return RedirectToAction("Login");
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.Email == user.Email);
            return (order == null || order.Status != "Pending") ? RedirectToAction("Profile") : View(order);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            var user = await _userManager.GetUserAsync(User!);
            if (user == null || avatarFile == null) return RedirectToAction("Profile");
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(avatarFile.FileName);
            var filePath = Path.Combine(_env.WebRootPath, "uploads", fileName);
            using (var stream = new FileStream(filePath, FileMode.Create)) { await avatarFile.CopyToAsync(stream); }
            user.AvatarUrl = "/uploads/" + fileName;
            await _userManager.UpdateAsync(user);
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var user = await _userManager.GetUserAsync(User!);
            var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id && o.Email == user!.Email);
            return order == null ? NotFound() : View("PrintInvoice", order);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction(nameof(ForgotPasswordConfirmation));
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action("ResetPassword", "Account", new { email = user.Email, token }, Request.Scheme);
            await _emailService.SendEmailAsync(user.Email!, "Reset Password", $"Link: {resetLink}");
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token) => View(new ResetPasswordViewModel { Email = email, Token = token });

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction("Login");
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            return result.Succeeded ? RedirectToAction(nameof(ResetPasswordConfirmation)) : View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation() => View();
    }
}