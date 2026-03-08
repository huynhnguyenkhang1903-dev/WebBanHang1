using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Websitebanhang.Models;
using Websitebanhang.Repositores;
using X.PagedList;
using X.PagedList.Extensions;

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

        // =====================================
        // USER PAGE - LIST PRODUCT
        // =====================================
        [AllowAnonymous]
        public IActionResult Index(string search, string price, string country, int page = 1)
        {
            var products = _productRepository.GetAll().AsQueryable();

            // search theo tên
            if (!string.IsNullOrEmpty(search))
            {
                products = products.Where(p =>
                    p.Name.ToLower().Contains(search.ToLower()));
            }

            // lọc theo giá
            if (!string.IsNullOrEmpty(price))
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

            // lọc theo quốc gia
            if (!string.IsNullOrEmpty(country))
            {
                products = products.Where(p => p.Country == country);
            }

            int pageSize = 8;

            return View(products.ToPagedList(page, pageSize));
        }

        // =====================================
        // USER PAGE - PRODUCT DETAIL
        // =====================================
        [AllowAnonymous]
        public IActionResult Display(int id)
        {
            var product = _productRepository.GetById(id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // =====================================
        // ADMIN PAGE - MANAGE PRODUCT
        // =====================================
        [Authorize(Roles = "Admin")]
        public IActionResult Manage()
        {
            var products = _productRepository.GetAll();
            return View(products);
        }

        // =====================================
        // ADMIN - ADD PRODUCT
        // =====================================
        [Authorize(Roles = "Admin")]
        public IActionResult Add()
        {
            LoadCategories();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Add(Product product, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    product.ImageUrl = await SaveImage(imageFile);
                }

                _productRepository.Add(product);

                return RedirectToAction(nameof(Manage));
            }

            LoadCategories();
            return View(product);
        }

        // =====================================
        // ADMIN - UPDATE PRODUCT
        // =====================================
        [Authorize(Roles = "Admin")]
        public IActionResult Update(int id)
        {
            var product = _productRepository.GetById(id);

            if (product == null)
                return NotFound();

            LoadCategories();

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(Product product, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    product.ImageUrl = await SaveImage(imageFile);
                }

                _productRepository.Update(product);

                return RedirectToAction(nameof(Manage));
            }

            LoadCategories();
            return View(product);
        }

        // =====================================
        // ADMIN - DELETE PRODUCT
        // =====================================
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var product = _productRepository.GetById(id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            _productRepository.Delete(id);

            return RedirectToAction(nameof(Manage));
        }

        // =====================================
        // LOAD CATEGORY
        // =====================================
        private void LoadCategories()
        {
            var categories = _categoryRepository.GetAll();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
        }

        // =====================================
        // SAVE IMAGE
        // =====================================
        private async Task<string> SaveImage(IFormFile image)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return "/images/" + fileName;
        }
    }
}