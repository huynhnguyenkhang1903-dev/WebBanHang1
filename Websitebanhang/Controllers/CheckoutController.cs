using Microsoft.AspNetCore.Mvc;

namespace Websitebanhang.Controllers
{
    public class CheckoutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}