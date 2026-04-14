using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using System.Linq;

namespace Websitebanhang.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // 1. Get Newest Products (Top 6)
            var newProducts = _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .Take(6)
                .ToList();

            ViewBag.NewProducts = newProducts;

            // 2. Get Best Selling Products (Top 6)
            // Flatten the items in all orders, group by ProductId, order by quantity, take top 6, then fetch the products.
            try 
            {
                var bestSellingProductIds = _context.Orders
                    .Include(o => o.Items)
                    .SelectMany(o => o.Items)
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(i => i.Quantity) })
                    .OrderByDescending(g => g.TotalQuantity)
                    .Take(6)
                    .Select(g => g.ProductId)
                    .ToList();

                var bestSellers = _context.Products
                    .Include(p => p.Category)
                    .Where(p => bestSellingProductIds.Contains(p.Id))
                    .ToList();

                // Sort the fetched products to match the best-selling order
                bestSellers = bestSellers.OrderBy(p => bestSellingProductIds.IndexOf(p.Id)).ToList();
                
                // Fallback: If no orders exist, just show 6 oldest or random products so the UI doesn't break
                if (!bestSellers.Any())
                {
                    bestSellers = _context.Products.Include(p => p.Category).Take(6).ToList();
                }

                ViewBag.BestSellers = bestSellers;
            }
            catch
            {
                // Safety net in case CartItem isn't queryable this way if it's stored as JSON
                // Fallback to basic products
                ViewBag.BestSellers = _context.Products.Include(p => p.Category).Take(6).ToList();
            }

            return View();
        }
    }
}