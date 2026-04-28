using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Websitebanhang.Repositores;
using X.PagedList;
using X.PagedList.Extensions;
using Microsoft.AspNetCore.Identity;
using Websitebanhang.Models;
using Websitebanhang.Data;
// alias
using ProductModel = Websitebanhang.Models.Product;

namespace Websitebanhang.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            UserManager<ApplicationUser> userManager,
            AppDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _userManager = userManager;
            _context = context;
        }

        [AllowAnonymous]
        public IActionResult Index(
            string search,
            string price,
            string country,
            int? categoryId,
            string sortOrder,
            int page = 1)
        {
            var products = _productRepository.GetAll().AsQueryable();

            // ================= FILTER =================

            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
            {
                if (search.Trim().Equals("admin", System.StringComparison.OrdinalIgnoreCase))
                    return RedirectToAction("Index", "Admin");

                products = products.Where(p =>
                    p.Name != null &&
                    p.Name.ToLower().Contains(search.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(price))
            {
                switch (price)
                {
                    case "low":
                        products = products.Where(p => p.Price < 100000);
                        break;
                    case "medium":
                        products = products.Where(p => p.Price >= 100000 && p.Price <= 300000);
                        break;
                    case "high":
                        products = products.Where(p => p.Price > 300000);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(country))
            {
                products = products.Where(p =>
                    p.Country != null && p.Country == country);
            }

            // ================= SORT =================

            products = sortOrder switch
            {
                "name" => products.OrderBy(p => p.Name),
                "name_desc" => products.OrderByDescending(p => p.Name),
                "price" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                _ => products.OrderByDescending(p => p.Id)
            };

            // ================= PAGING =================

            int pageSize = 8;

            var pagedProducts = products.ToPagedList(page, pageSize);

            return View(pagedProducts);
        }

        [AllowAnonymous]
        public IActionResult Display(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Đánh giá không hợp lệ.";
                return RedirectToAction("Display", new { id = productId });
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["Error"] = "Vui lòng nhập bình luận.";
                return RedirectToAction("Display", new { id = productId });
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = user.Id,
                Rating = rating,
                Comment = comment,
                CreatedAt = System.DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction("Display", new { id = productId });
        }

        // ================= ADMIN =================

        [Authorize(Roles = "Admin")]
        public IActionResult Manage()
        {
            var products = _productRepository.GetAll();
            return View(products);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Add()
        {
            ViewBag.Categories = _categoryRepository.GetAll();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Add(ProductModel product)
        {
            if (ModelState.IsValid)
            {
                _productRepository.Add(product);
                return RedirectToAction("Manage");
            }

            ViewBag.Categories = _categoryRepository.GetAll();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            ViewBag.Categories = _categoryRepository.GetAll();
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Update(ProductModel product)
        {
            if (ModelState.IsValid)
            {
                _productRepository.Update(product);
                return RedirectToAction("Manage");
            }

            ViewBag.Categories = _categoryRepository.GetAll();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            _productRepository.Delete(id);
            return RedirectToAction("Manage");
        }
    }
}