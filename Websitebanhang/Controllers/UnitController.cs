using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Data;
using Websitebanhang.Models;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UnitController : Controller
    {
        private readonly AppDbContext _context;

        public UnitController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var units = _context.UnitsOfMeasure.ToList();
            return View(units);
        }

        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(UnitOfMeasure model)
        {
            if (ModelState.IsValid)
            {
                _context.UnitsOfMeasure.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public IActionResult Update(int id)
        {
            var unit = _context.UnitsOfMeasure.Find(id);
            if (unit == null) return NotFound();
            return View(unit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(UnitOfMeasure model)
        {
            if (ModelState.IsValid)
            {
                _context.UnitsOfMeasure.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public IActionResult Delete(int id)
        {
            var unit = _context.UnitsOfMeasure.Find(id);
            if (unit == null) return NotFound();
            return View(unit);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var unit = _context.UnitsOfMeasure.Find(id);
            if (unit != null)
            {
                _context.UnitsOfMeasure.Remove(unit);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
