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

namespace Websitebanhang.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly Websitebanhang.Data.AppDbContext _context;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IEmailService emailService, IWebHostEnvironment env, Websitebanhang.Data.AppDbContext context)
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
        [Authorize]
        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // NEW: OrderDetails
        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            // Allow admins or the owner (by UserId or Email) to view
            if (!isAdmin)
            {
                if (!string.Equals(order.UserId, user.Id, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(order.Email, user.Email, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "Bạn không có quyền xem đơn hàng này.";
                    return RedirectToAction("Profile");
                }
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
                    var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);
                    if (result.Succeeded)
                    {
                        var roles = await _userManager.GetRolesAsync(user);

                        if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
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
                    DateOfBirth = model.DateOfBirth
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Default role for new users
                    await _userManager.AddToRoleAsync(user, "User");
                    return RedirectToAction("Login", "Account");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Email == user.Email)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var model = new ProfileViewModel
            {
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Orders = orders
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            user.FullName = string.IsNullOrEmpty(model.FullName) ? "" : model.FullName;
            user.Address = string.IsNullOrEmpty(model.Address) ? "" : model.Address;
            user.PhoneNumber = model.PhoneNumber;
            user.DateOfBirth = model.DateOfBirth;

            if (model.UserName != null && model.UserName != user.UserName)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
                await _signInManager.RefreshSignInAsync(user);
            }

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Bạn đã cập nhật hồ sơ thành công!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

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
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // Refresh sign-in to keep user logged in after password change
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Mật khẩu của bạn đã được đổi thành công!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var callbackUrl = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, protocol: HttpContext.Request.Scheme);

                    string htmlBody = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; padding: 20px; border: 1px solid #ddd;'>
                            <h2 style='color: #6F4E37;'>Khôi Phục Mật Khẩu</h2>
                            <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản tại Coffee Shop. Vui lòng nhấp vào nút bên dưới để khôi phục:</p>
                            <a href='{callbackUrl}' style='display: inline-block; padding: 10px 20px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px;'>Đặt lại mật khẩu</a>
                            <p style='margin-top: 20px; font-size: 12px; color: #777;'>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>
                        </div>
                    ";

                    await _emailService.SendEmailAsync(model.Email, "Khôi phục mật khẩu - Coffee Shop", htmlBody);
                }

                TempData["SuccessMessage"] = "Nếu email tồn tại trong hệ thống, thư chứa link xác nhận đã được gửi đi. Vui lòng kiểm tra hộp thư/spam.";
                return RedirectToAction("ForgotPassword");
            }

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Mật khẩu của bạn đã được cập nhật thành công! Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> UploadAvatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            ViewBag.CurrentAvatar = user.AvatarUrl;
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            if (avatarFile == null || avatarFile.Length == 0)
            {
                ModelState.AddModelError(string.Empty, "Vui lòng chọn một tệp hình ảnh hợp lệ.");
                ViewBag.CurrentAvatar = user.AvatarUrl;
                return View();
            }

            // Check size (2MB)
            if (avatarFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(string.Empty, "Kích thước tệp không được vượt quá 2MB.");
                ViewBag.CurrentAvatar = user.AvatarUrl;
                return View();
            }

            // Check extension
            var ext = Path.GetExtension(avatarFile.FileName).ToLower();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png")
            {
                ModelState.AddModelError(string.Empty, "Chỉ cho phép tệp hình ảnh định dạng (jpg, jpeg, png).");
                ViewBag.CurrentAvatar = user.AvatarUrl;
                return View();
            }

            // Save file
            var folderPath = Path.Combine(_env.WebRootPath, "images", "avatars");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + avatarFile.FileName;
            var filePath = Path.Combine(folderPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }

            // Delete old avatar if not default
            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var oldFilePath = Path.Combine(_env.WebRootPath, user.AvatarUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            user.AvatarUrl = "/images/avatars/" + uniqueFileName;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "Ảnh đại diện đã được cập nhật thành công!";
            return RedirectToAction("Profile");
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && (o.Email == user.Email || o.UserId == user.Id));

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền xem.";
                return RedirectToAction("Profile", "Account");
            }

            return View(order);
        }

        // ================= HỦY ĐƠN HÀNG =================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            // 🔥 FIX NULL EMAIL
            if (string.IsNullOrEmpty(user.Email))
            {
                TempData["Error"] = "Tài khoản không hợp lệ!";
                return RedirectToAction("Profile");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.Email == user.Email);

            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Profile");
            }

            // 🔥 CHỈ CHO HỦY KHI PENDING
            if (order.Status != "Pending")
            {
                TempData["Error"] = "Chỉ có thể hủy đơn đang chờ xử lý!";
                return RedirectToAction("Profile");
            }

            // ✅ Hủy đơn
            order.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công!";
            return RedirectToAction("Profile");
        }
    }
}