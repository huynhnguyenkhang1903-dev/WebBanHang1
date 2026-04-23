using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System.Linq;

namespace Websitebanhang.Controllers
{
    public class PromotionController : Controller
    {
        private readonly AppDbContext _context;

        public PromotionController(AppDbContext context)
        {
            _context = context;
        }

        // READ
        public IActionResult Index()
        {
            return View(_context.Promotions.ToList());
        }

        // CREATE
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Promotion model)
        {
            if (ModelState.IsValid)
            {
                _context.Promotions.Add(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // UPDATE
        public IActionResult Edit(int id)
        {
            var promo = _context.Promotions.Find(id);
            if (promo == null) return NotFound();
            return View(promo);
        }

        [HttpPost]
        public IActionResult Edit(Promotion model)
        {
            if (ModelState.IsValid)
            {
                _context.Promotions.Update(model);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // DELETE
        public IActionResult Delete(int id)
        {
            var promo = _context.Promotions.Find(id);
            if (promo == null) return NotFound();
            return View(promo);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var promo = _context.Promotions.Find(id);
            if (promo != null)
            {
                _context.Promotions.Remove(promo);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}