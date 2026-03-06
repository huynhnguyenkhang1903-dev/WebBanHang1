using Microsoft.AspNetCore.Mvc;
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
    }
}