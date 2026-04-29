using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSettingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminSettingController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.WebsiteSettings.ToListAsync();
            
            // Ensure basic settings exist
            string[,] defaults = {
                { "Logo", "/images/logo.png", "Website Logo URL" },
                { "SiteTitle", "Aura Coffee", "Tiêu đề website" },
                { "Hotline", "0344.506.553", "Số điện thoại hỗ trợ" },
                { "Email", "support@auracoffee.com", "Email liên hệ" },
                { "Address", "123 Đường Cà Phê, TP. Hồ Chí Minh", "Địa chỉ trụ sở" },
                { "Facebook", "https://facebook.com/auracoffee", "Link Fanpage Facebook" }
            };

            bool changed = false;
            for (int i = 0; i < defaults.GetLength(0); i++)
            {
                var key = defaults[i, 0];
                if (!settings.Any(s => s.Key == key))
                {
                    _context.WebsiteSettings.Add(new WebsiteSetting { 
                        Key = key, 
                        Value = defaults[i, 1], 
                        Description = defaults[i, 2] 
                    });
                    changed = true;
                }
            }

            if (changed)
            {
                await _context.SaveChangesAsync();
                settings = await _context.WebsiteSettings.ToListAsync();
            }

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateLogo(IFormFile logoFile)
        {
            if (logoFile != null && logoFile.Length > 0)
            {
                var fileName = "logo" + Path.GetExtension(logoFile.FileName);
                var uploadPath = Path.Combine(_env.WebRootPath, "images");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                var logoSetting = await _context.WebsiteSettings.FirstOrDefaultAsync(s => s.Key == "Logo");
                if (logoSetting == null)
                {
                    logoSetting = new WebsiteSetting { Key = "Logo", Description = "Website Logo URL" };
                    _context.WebsiteSettings.Add(logoSetting);
                }

                logoSetting.Value = "/images/" + fileName + "?v=" + DateTime.Now.Ticks; // Add version to bust cache
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật Logo thành công!";
            }
            else
            {
                TempData["Error"] = "Vui lòng chọn file ảnh!";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSetting(string key, string value)
        {
            var setting = await _context.WebsiteSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting != null)
            {
                setting.Value = value;
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Cập nhật {key} thành công!";
            }
            return RedirectToAction("Index");
        }
    }
}
