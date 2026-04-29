using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System.IO;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminProductImageController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminProductImageController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            ViewBag.Product = product;
            var images = await _context.ProductImages
                .Where(img => img.ProductId == productId)
                .OrderBy(img => img.OrderIndex)
                .ToListAsync();

            return View(images);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int productId, List<IFormFile> imageFiles)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            if (imageFiles != null && imageFiles.Count > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                int maxOrder = await _context.ProductImages
                    .Where(img => img.ProductId == productId)
                    .Select(img => (int?)img.OrderIndex)
                    .MaxAsync() ?? 0;

                foreach (var file in imageFiles)
                {
                    if (file.Length > 0)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        maxOrder++;
                        var newImg = new ProductImage
                        {
                            ProductId = productId,
                            ImageUrl = "/uploads/products/" + uniqueFileName,
                            OrderIndex = maxOrder
                        };
                        _context.ProductImages.Add(newImg);
                    }
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã tải lên {imageFiles.Count} hình ảnh thành công.";
            }
            else
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một hình ảnh.";
            }

            return RedirectToAction(nameof(Index), new { productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(int id, int orderIndex, int productId)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image != null && image.ProductId == productId)
            {
                image.OrderIndex = orderIndex;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật thứ tự hình ảnh.";
            }
            return RedirectToAction(nameof(Index), new { productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int productId)
        {
            var image = await _context.ProductImages.FindAsync(id);
            if (image != null && image.ProductId == productId)
            {
                // Optional: Delete physical file
                var filePath = Path.Combine(_env.WebRootPath, image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa hình ảnh.";
            }
            return RedirectToAction(nameof(Index), new { productId });
        }
    }
}
