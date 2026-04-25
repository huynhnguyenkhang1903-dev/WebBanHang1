using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Websitebanhang.Repositores;
using X.PagedList;
using X.PagedList.Extensions;
// alias
using ProductModel = Websitebanhang.Models.Product;

namespace Websitebanhang.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
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