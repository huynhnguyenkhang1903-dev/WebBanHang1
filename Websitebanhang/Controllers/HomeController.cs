using Microsoft.AspNetCore.Mvc;

namespace Websitebanhang.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}