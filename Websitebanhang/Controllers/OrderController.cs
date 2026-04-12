using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Data;
using System.Linq;

namespace Websitebanhang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var orders = _context.Orders.OrderByDescending(o => o.OrderDate).ToList();
            return View(orders);
        }
    }
}