using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using X.PagedList;
using X.PagedList.Extensions;
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
    }
}
