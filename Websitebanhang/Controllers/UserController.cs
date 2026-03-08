using Microsoft.AspNetCore.Mvc;

namespace Websitebanhang.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}