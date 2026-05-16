using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using X.PagedList;
using X.PagedList.Extensions;
using Websitebanhang.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminInventoryController : Controller
    {
        private readonly AppDbContext _context;

        public AdminInventoryController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> History(int? productId, string type, int page = 1)
        {
            int pageSize = 20;
            var query = _context.StockHistories
                .Include(h => h.Product)
                .Include(h => h.User)
                .OrderByDescending(h => h.CreatedAt)
                .AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(h => h.ProductId == productId);
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(h => h.Type == type);
            }

            var history = query.ToPagedList(page, pageSize);

            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            ViewBag.SelectedProductId = productId;
            ViewBag.SelectedType = type;

            return View(history);
        }
        [HttpGet]
        public async Task<IActionResult> AdjustStock()
        {
            ViewBag.Products = await _context.Products.OrderBy(p => p.Name).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int productId, int quantity, string type, string note)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();

            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0.";
                return RedirectToAction("AdjustStock");
            }

            // Xử lý loại thay đổi
            if (type == "Xuất kho")
            {
                if (product.Stock < quantity)
                {
                    TempData["Error"] = "Số lượng tồn kho không đủ để xuất.";
                    return RedirectToAction("AdjustStock");
                }
                product.Stock -= quantity;
            }
            else // Nhập kho
            {
                product.Stock += quantity;
            }

            // Lưu lịch sử
            var history = new StockHistory
            {
                ProductId = productId,
                QuantityChange = (type == "Xuất kho" ? -quantity : quantity),
                BalanceAfter = product.Stock,
                Type = type,
                Note = string.IsNullOrEmpty(note) ? "Điều chỉnh thủ công bởi Admin" : note,
                CreatedAt = System.DateTime.Now,
                UserId = (await _context.Users.FirstOrDefaultAsync(u => u.UserName == User.Identity.Name))?.Id
            };

            _context.StockHistories.Add(history);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã {type} thành công sản phẩm: {product.Name}. Tồn kho hiện tại: {product.Stock}";
            return RedirectToAction("History");
        }
    }
}
