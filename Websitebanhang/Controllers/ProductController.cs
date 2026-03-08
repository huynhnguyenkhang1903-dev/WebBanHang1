using Microsoft.AspNetCore.Mvc;
<<<<<<< HEAD
using Websitebanhang.Models;
using Websitebanhang.Repositores;

namespace Websitebanhang.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;

        public ProductController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // LIST PRODUCT
        public IActionResult Index()
        {
            var products = _productRepository.GetAll();
            return View(products);
        }

        // DETAILS
        public IActionResult Display(int id)
        {
            var product = _productRepository.GetById(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // ADD (GET)
        public IActionResult Add()
        {
            return View();
        }

        // ADD (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Product product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            _productRepository.Add(product);
            return RedirectToAction(nameof(Index));
        }

        // UPDATE (GET)
        public IActionResult Update(int id)
        {
            var product = _productRepository.GetById(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // UPDATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(Product product)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            _productRepository.Update(product);
            return RedirectToAction(nameof(Index));
        }

        // DELETE (GET)
        public IActionResult Delete(int id)
        {
            var product = _productRepository.GetById(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // DELETE (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _productRepository.Delete(id);
            return RedirectToAction(nameof(Index));
        }
=======
using Microsoft.AspNetCore.Mvc.Rendering;
using Websitebanhang.Models;
using Websitebanhang.Repositores;

public class ProductController : Controller
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public ProductController(IProductRepository productRepository,
                             ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
    }

    // LIST PRODUCT
    public IActionResult Index()
    {
        var products = _productRepository.GetAll();
        return View(products);
    }

    // ADD PRODUCT
    public IActionResult Add()
    {
        var categories = _categoryRepository.GetAll();
        ViewBag.Categories = new SelectList(categories, "Id", "Name");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Add(Product product, IFormFile
imageUrl, List<IFormFile> imageUrls)
    {
        if (ModelState.IsValid)
        {
            if (imageUrl != null)
            {
                // Lưu hình ảnh đại diện
                product.ImageUrl = await SaveImage(imageUrl);
            }
            if (imageUrls != null)
            {
                product.ImageUrls = new List<string>();
                foreach (var file in imageUrls)
                {
                    // Lưu các hình ảnh khác
                    product.ImageUrls.Add(await SaveImage(file));
                }
            }
            _productRepository.Add(product);
            return RedirectToAction("Index");
        }
        return View(product);
    }
    private async Task<string> SaveImage(IFormFile image)
    {
        // Thay đổi đường dẫn theo cấu hình của bạn
        var savePath = Path.Combine("wwwroot/images", image.FileName);
        using (var fileStream = new FileStream(savePath, FileMode.Create))
        {
            await image.CopyToAsync(fileStream);
        }
        return "/images/" + image.FileName; // Trả về đường dẫn tương đối
    }

    // DISPLAY PRODUCT
    public IActionResult Display(int id)
    {
        var product = _productRepository.GetById(id);
        if (product == null)
            return NotFound();

        return View(product);
    }

    // UPDATE PRODUCT
    public IActionResult Update(int id)
    {
        var product = _productRepository.GetById(id);
        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpPost]
    public IActionResult Update(Product product)
    {
        if (ModelState.IsValid)
        {
            _productRepository.Update(product);
            return RedirectToAction("Index");
        }

        return View(product);
    }

    // DELETE PRODUCT
    public IActionResult Delete(int id)
    {
        var product = _productRepository.GetById(id);
        if (product == null)
            return NotFound();

        return View(product);
    }

    [HttpPost, ActionName("Delete")]
    public IActionResult DeleteConfirmed(int id)
    {
        _productRepository.Delete(id);
        return RedirectToAction("Index");
>>>>>>> ee325eaf63f2aabb046ebc4c33770f92d4a56eca
    }
}