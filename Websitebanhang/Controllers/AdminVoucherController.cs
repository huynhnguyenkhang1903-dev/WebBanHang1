using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System.Linq;
using System.Threading.Tasks;
using Websitebanhang.Services;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminVoucherController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IActivityLogService _activityLogService;

        public AdminVoucherController(AppDbContext context, IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        public async Task<IActionResult> Index()
        {
            var vouchers = await _context.Voucher.ToListAsync();
            return View(vouchers);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                _context.Voucher.Add(voucher);
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Thêm Voucher", "Voucher", voucher.Id.ToString(), $"Admin đã thêm voucher: {voucher.Code}");
                TempData["Success"] = "Đã thêm voucher mới.";
                return RedirectToAction(nameof(Index));
            }
            return View(voucher);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _context.Voucher.FindAsync(id);
            if (voucher == null) return NotFound();
            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                _context.Voucher.Update(voucher);
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Cập nhật Voucher", "Voucher", voucher.Id.ToString(), $"Admin đã cập nhật voucher: {voucher.Code}");
                TempData["Success"] = "Đã cập nhật voucher.";
                return RedirectToAction(nameof(Index));
            }
            return View(voucher);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Voucher.FindAsync(id);
            if (voucher != null)
            {
                // Check if any product is using this voucher
                var isUsed = await _context.Products.AnyAsync(p => p.VoucherId == id);
                if (isUsed)
                {
                    TempData["Error"] = "Không thể xóa voucher đang được áp dụng cho sản phẩm!";
                }
                else
                {
                    string code = voucher.Code;
                    _context.Voucher.Remove(voucher);
                    await _context.SaveChangesAsync();
                    await _activityLogService.LogAsync("Xóa Voucher", "Voucher", id.ToString(), $"Admin đã xóa voucher: {code}");
                    TempData["Success"] = "Đã xóa voucher.";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
