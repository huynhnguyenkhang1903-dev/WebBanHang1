using Microsoft.AspNetCore.Mvc;

namespace Websitebanhang.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}