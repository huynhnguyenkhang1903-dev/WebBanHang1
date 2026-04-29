using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSupplierController : Controller
    {
        private readonly AppDbContext _context;
        private readonly Services.IActivityLogService _activityLogService;

        public AdminSupplierController(AppDbContext context, Services.IActivityLogService activityLogService)
        {
            _context = context;
            _activityLogService = activityLogService;
        }

        public async Task<IActionResult> Index()
        {
            var suppliers = await _context.Suppliers.Include(s => s.Products).ToListAsync();
            return View(suppliers);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Thêm NCC", "Supplier", supplier.Id.ToString(), $"Admin đã thêm nhà cung cấp: {supplier.Name}");
                TempData["Success"] = "Đã thêm nhà cung cấp mới.";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                _context.Suppliers.Update(supplier);
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Cập nhật NCC", "Supplier", supplier.Id.ToString(), $"Admin đã cập nhật nhà cung cấp: {supplier.Name}");
                TempData["Success"] = "Đã cập nhật nhà cung cấp.";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var supplier = await _context.Suppliers.Include(s => s.Products).FirstOrDefaultAsync(s => s.Id == id);
            if (supplier != null)
            {
                if (supplier.Products.Any())
                {
                    TempData["Error"] = "Không thể xóa nhà cung cấp đang có sản phẩm liên kết!";
                }
                else
                {
                    string supplierName = supplier.Name;
                    _context.Suppliers.Remove(supplier);
                    await _context.SaveChangesAsync();
                    await _activityLogService.LogAsync("Xóa NCC", "Supplier", id.ToString(), $"Admin đã xóa nhà cung cấp: {supplierName}");
                    TempData["Success"] = "Đã xóa nhà cung cấp.";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
