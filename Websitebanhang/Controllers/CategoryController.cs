using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Models;
using Websitebanhang.Repositores;

namespace Websitebanhang.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public IActionResult Index()
        {
            var categories = _categoryRepository.GetAll();
            return View(categories);
        }

        [Authorize(Roles = "Admin,NhanVien")]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,NhanVien")]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Category category)
        {
            if (ModelState.IsValid)
            {
                _categoryRepository.Add(category);
                return RedirectToAction("Index");
            }
            return View(category);
        }

        [Authorize(Roles = "Admin,NhanVien")]
        public IActionResult Update(int id)
        {
            var category = _categoryRepository.GetById(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,NhanVien")]
        [ValidateAntiForgeryToken]
        public IActionResult Update(Category category)
        {
            if (ModelState.IsValid)
            {
                _categoryRepository.Update(category);
                return RedirectToAction("Index");
            }
            return View(category);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var category = _categoryRepository.GetById(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            _categoryRepository.Delete(id);
            return RedirectToAction("Index");
        }
    }
}
