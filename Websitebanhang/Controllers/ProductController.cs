using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        private readonly Services.IActivityLogService _activityLogService;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            Services.IActivityLogService activityLogService)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _userManager = userManager;
            _context = context;
            _activityLogService = activityLogService;
        }

        [AllowAnonymous]
        public IActionResult Index(
            string search,
            string price,
            string country,
            bool? hasPromotion,
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

            if (hasPromotion == true)
            {
                products = products.Where(p => p.VoucherId != null);
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

            // Lấy sản phẩm liên quan (cùng danh mục, tối đa 4 sản phẩm, loại trừ chính nó)
            var relatedProducts = _productRepository.GetAll()
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            // 🔥 Ghi nhận lịch sử xem sản phẩm
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(userId))
                {
                    var existingHistory = _context.ProductViewHistories
                        .FirstOrDefault(h => h.UserId == userId && h.ProductId == id);

                    if (existingHistory != null)
                    {
                        existingHistory.ViewedAt = DateTime.Now;
                    }
                    else
                    {
                        _context.ProductViewHistories.Add(new ProductViewHistory
                        {
                            UserId = userId,
                            ProductId = id,
                            ViewedAt = DateTime.Now
                        });
                    }
                    _context.SaveChanges();
                }
            }

            return View(product);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult SearchSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return Json(new object[] { });
            }

            var suggestions = _context.Products
                .Where(p => p.Name.Contains(term))
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    imageUrl = p.ImageUrl
                })
                .Take(5)
                .ToList();

            return Json(suggestions);
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
                CreatedAt = System.DateTime.Now,
                IsApproved = false  // Cần Admin duyệt trước khi hiển thị
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cảm ơn bạn đã đánh giá! Bình luận của bạn đang chờ Admin duyệt.";
            return RedirectToAction("Display", new { id = productId });
        }

        // ================= BÁO CÁO BÌNH LUẬN =================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReportReview(int reviewId, int productId, string reason)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return NotFound();

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["Error"] = "Vui lòng nhập lý do báo cáo.";
                return RedirectToAction("Display", new { id = productId });
            }

            review.IsReported = true;
            review.ReportReason = reason.Trim();
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi báo cáo bình luận. Cảm ơn bạn!";
            return RedirectToAction("Display", new { id = productId });
        }

        // ================= ADMIN =================

        [Authorize(Roles = "Admin")]
        public IActionResult Manage(int page = 1)
        {
            int pageSize = 10;
            var products = _productRepository.GetAll().ToPagedList(page, pageSize);
            return View(products);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Add()
        {
            ViewBag.Categories = _categoryRepository.GetAll();
            ViewBag.Suppliers = _context.Suppliers.ToList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(ProductModel product)
        {
            if (ModelState.IsValid)
            {
                _productRepository.Add(product);

                // Log history
                var history = new StockHistory
                {
                    ProductId = product.Id,
                    QuantityChange = product.Stock,
                    BalanceAfter = product.Stock,
                    Type = "Nhập kho",
                    Note = "Khởi tạo sản phẩm",
                    CreatedAt = DateTime.Now,
                    UserId = (await _userManager.GetUserAsync(User))?.Id
                };
                _context.StockHistories.Add(history);
                await _context.SaveChangesAsync();
                await _activityLogService.LogAsync("Thêm SP", "Product", product.Id.ToString(), $"Admin đã thêm sản phẩm: {product.Name}");

                return RedirectToAction("Manage");
            }

            ViewBag.Categories = _categoryRepository.GetAll();
            ViewBag.Suppliers = _context.Suppliers.ToList();
            return View(product);
        }

        public IActionResult Update(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            ViewBag.Categories = _categoryRepository.GetAll();
            ViewBag.Suppliers = _context.Suppliers.ToList();
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(ProductModel product)
        {
            if (ModelState.IsValid)
            {
                // Lấy stock cũ trước khi update
                var oldProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
                int oldStock = oldProduct?.Stock ?? 0;

                _productRepository.Update(product);

                // Nếu có thay đổi stock thì log
                if (product.Stock != oldStock)
                {
                    int diff = product.Stock - oldStock;
                    var history = new StockHistory
                    {
                        ProductId = product.Id,
                        QuantityChange = diff,
                        BalanceAfter = product.Stock,
                        Type = diff > 0 ? "Nhập thêm" : "Điều chỉnh giảm",
                        Note = "Admin điều chỉnh thủ công",
                        CreatedAt = DateTime.Now,
                        UserId = (await _userManager.GetUserAsync(User))?.Id
                    };
                    _context.StockHistories.Add(history);
                    await _context.SaveChangesAsync();
                }
                await _activityLogService.LogAsync("Cập nhật SP", "Product", product.Id.ToString(), $"Admin đã cập nhật sản phẩm: {product.Name}");

                return RedirectToAction("Manage");
            }

            ViewBag.Categories = _categoryRepository.GetAll();
            ViewBag.Suppliers = _context.Suppliers.ToList();
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            return View(product);
        }

        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = _productRepository.GetById(id);
            string productName = product?.Name ?? "Unknown";
            _productRepository.Delete(id);
            await _activityLogService.LogAsync("Xóa SP", "Product", id.ToString(), $"Admin đã xóa sản phẩm: {productName}");
            return RedirectToAction("Manage");
        }
    }
}