using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Services;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PromotionController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;

        public PromotionController(AppDbContext context, IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        // READ
        public async Task<IActionResult> Index()
        {
            var promotions = await _context.Promotions.OrderByDescending(p => p.StartDate).ToListAsync();
            return View(promotions);
        }

        // CREATE
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Promotion model)
        {
            if (ModelState.IsValid)
            {
                _context.Promotions.Add(model);
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Thêm Khuyến mãi", "Promotion", model.Id.ToString(), $"Admin đã thêm chương trình khuyến mãi: {model.Name}");
                TempData["Success"] = "Đã thêm chương trình khuyến mãi mới.";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // UPDATE
        public async Task<IActionResult> Edit(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();
            return View(promo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Promotion model)
        {
            if (ModelState.IsValid)
            {
                _context.Promotions.Update(model);
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Cập nhật Khuyến mãi", "Promotion", model.Id.ToString(), $"Admin đã cập nhật chương trình khuyến mãi: {model.Name}");
                TempData["Success"] = "Đã cập nhật chương trình khuyến mãi.";
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // DELETE (Using Post for direct deletion from Index or a separate Delete view)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo != null)
            {
                string name = promo.Name;
                _context.Promotions.Remove(promo);
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Xóa Khuyến mãi", "Promotion", id.ToString(), $"Admin đã xóa chương trình khuyến mãi: {name}");
                TempData["Success"] = "Đã xóa chương trình khuyến mãi.";
            }
            return RedirectToAction("Index");
        }
    }
}