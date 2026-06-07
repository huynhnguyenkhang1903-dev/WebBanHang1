using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin,NhanVien")]
    public class AdminBannerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminBannerController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners.OrderBy(b => b.OrderIndex).ToListAsync();
            return View(banners);
        }

        public IActionResult Create()
        {
            return View(new Banner { IsActive = true, OrderIndex = 0 });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner model, IFormFile? imageFile)
        {
            if (imageFile != null)
            {
                ModelState.Remove("ImageUrl");
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "banners");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    model.ImageUrl = "/uploads/banners/" + uniqueFileName;
                }
                else if (string.IsNullOrEmpty(model.ImageUrl))
                {
                    ModelState.AddModelError("ImageUrl", "Vui lòng tải lên hoặc nhập link ảnh.");
                    return View(model);
                }

                _context.Banners.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm banner thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banner model, IFormFile? imageFile)
        {
            if (id != model.Id) return NotFound();

            // We must remove validation for ImageUrl if a new file is uploaded
            if (imageFile != null)
            {
                ModelState.Remove("ImageUrl");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "banners");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        model.ImageUrl = "/uploads/banners/" + uniqueFileName;
                    }

                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đã cập nhật banner thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BannerExists(model.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(int id, int orderIndex)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                banner.OrderIndex = orderIndex;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật thứ tự banner.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa banner thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BannerExists(int id)
        {
            return _context.Banners.Any(e => e.Id == id);
        }
    }
}
