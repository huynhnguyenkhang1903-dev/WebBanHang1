using Microsoft.AspNetCore.Mvc;

namespace Websitebanhang.Controllers
{
    public class AdminController : Controller
    {
        private bool CheckAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        public IActionResult Users()
        {
            if (!CheckAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            return View(LoginController.users);
        }
    }
}