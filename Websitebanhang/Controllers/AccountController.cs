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
                        false);

                    if (result.Succeeded)
                    {
                        var roles = await _userManager.GetRolesAsync(user);

                        if (roles.Contains("Admin"))
                            return RedirectToAction("Index", "Admin");

                        return RedirectToAction("Index", "Home");
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
                    DateOfBirth = model.DateOfBirth
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }

            return View(model);
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
            order.CancelReason = cancelReason;
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
        public async Task<IActionResult> RequestReturn(int id, string returnReason)
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
                TempData["Error"] = "Chỉ có thể yêu cầu trả hàng cho đơn đã giao.";
                return RedirectToAction("OrderDetails", new { id });
            }

            if (string.IsNullOrWhiteSpace(returnReason))
            {
                TempData["Error"] = "Vui lòng nhập lý do trả hàng.";
                return RedirectToAction("OrderDetails", new { id });
            }

            order.Status = OrderStatus.Returned; // mark as returned/requested
            order.ReturnReason = returnReason;
            order.ReturnRequestedAt = DateTime.Now;

            // If paid by bank, we will let admin process refund (or could auto-refund depending on policy)
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Yêu cầu trả hàng đã được gửi. Admin sẽ xử lý.";
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
    }
}