using Microsoft.AspNetCore.Mvc;
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
    }
}