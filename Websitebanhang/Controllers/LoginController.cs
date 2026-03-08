using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Websitebanhang.Models;

namespace Websitebanhang.Controllers
{
    public class LoginController : Controller
    {
        // TÀI KHOẢN ADMIN MẶC ĐỊNH
        public static List<User> users = new List<User>()
        {
            new User
            {
                FullName = "Administrator",
                Email = "admin@gmail.com",
                Phone = "0000000000",
                Address = "System",
                Password = "123456",
                Role = "Admin",
                IsLocked = false
            }
        };

        // ================= LOGIN =================

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            var user = users.FirstOrDefault(x =>
                x.Email.ToLower() == email.ToLower()
                && x.Password == password);

            if (user == null)
            {
                ViewBag.Error = "Sai tài khoản hoặc mật khẩu";
                return View();
            }

            if (user.IsLocked)
            {
                ViewBag.Error = "Tài khoản đã bị khóa";
                return View();
            }

            // lưu session
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("Role", user.Role);

            return RedirectToAction("Index", "Home");
        }

        // ================= REGISTER =================

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string fullname, string email, string phone, string address, string password, string confirmpassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            if (users.Any(x => x.Email.ToLower() == email.ToLower()))
            {
                ViewBag.Error = "Mail này đã được tạo";
                return View();
            }

            if (password != confirmpassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không đúng";
                return View();
            }

            User u = new User
            {
                FullName = fullname,
                Email = email,
                Phone = phone,
                Address = address,
                Password = password,
                Role = "User",
                IsLocked = false
            };

            users.Add(u);

            TempData["Success"] = "Bạn đã đăng ký tài khoản thành công";

            return RedirectToAction("Login");
        }

        // ================= LOGOUT =================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}