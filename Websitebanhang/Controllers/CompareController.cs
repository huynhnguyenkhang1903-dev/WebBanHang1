using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Websitebanhang.Data;
using Websitebanhang.Helpers;

namespace Websitebanhang.Controllers
{
    public class CompareController : Controller
    {
        private readonly AppDbContext _context;

        public CompareController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => compareList.Contains(p.Id))
                .ToListAsync();

            // Sắp xếp lại theo thứ tự thêm vào
            var sortedProducts = compareList
                .Select(id => products.FirstOrDefault(p => p.Id == id))
                .Where(p => p != null)
                .ToList();

            return View(sortedProducts);
        }

        public IActionResult Add(int id, string returnUrl)
        {
            var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();

            if (compareList.Contains(id))
            {
                TempData["Info"] = "Sản phẩm đã có trong danh sách so sánh!";
            }
            else if (compareList.Count >= 3)
            {
                TempData["Error"] = "Chỉ có thể so sánh tối đa 3 sản phẩm cùng lúc!";
            }
            else
            {
                compareList.Add(id);
                HttpContext.Session.SetObject("CompareList", compareList);
                TempData["Success"] = "Đã thêm vào danh sách so sánh!";
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Remove(int id)
        {
            var compareList = HttpContext.Session.GetObject<List<int>>("CompareList") ?? new List<int>();

            if (compareList.Contains(id))
            {
                compareList.Remove(id);
                HttpContext.Session.SetObject("CompareList", compareList);
                TempData["Success"] = "Đã xóa khỏi danh sách so sánh!";
            }

            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            HttpContext.Session.Remove("CompareList");
            TempData["Success"] = "Đã xóa toàn bộ danh sách so sánh!";
            return RedirectToAction("Index");
        }
    }
}
