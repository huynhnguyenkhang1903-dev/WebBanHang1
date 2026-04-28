using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Websitebanhang.Models;
using Websitebanhang.Repositores;
using Websitebanhang.Helpers;

namespace Websitebanhang.Controllers
{
    public class WishlistController : Controller
    {
        private readonly IProductRepository _productRepository;

        public WishlistController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // ================= XEM DANH MỤC YÊU THÍCH =================
        public IActionResult Index()
        {
            var wishlist = HttpContext.Session.GetObject<List<CartItem>>("Wishlist") ?? new List<CartItem>();
            return View(wishlist);
        }

        // ================= THÊM VÀO YÊU THÍCH =================
        public IActionResult Add(int id, string returnUrl)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            var wishlist = HttpContext.Session.GetObject<List<CartItem>>("Wishlist") ?? new List<CartItem>();
            var item = wishlist.FirstOrDefault(p => p.ProductId == id);

            if (item == null)
            {
                wishlist.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name ?? "",
                    Price = product.Price,
                    Quantity = 1, // Not really used for wishlist, but CartItem requires it
                    ImageUrl = product.ImageUrl ?? ""
                });
                HttpContext.Session.SetObject("Wishlist", wishlist);
                TempData["Success"] = "Đã thêm vào mục yêu thích!";
            }
            else
            {
                TempData["Info"] = "Sản phẩm đã có trong mục yêu thích!";
            }

            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index");
        }

        // ================= XÓA KHỎI YÊU THÍCH =================
        public IActionResult Remove(int id)
        {
            var wishlist = HttpContext.Session.GetObject<List<CartItem>>("Wishlist") ?? new List<CartItem>();
            var item = wishlist.FirstOrDefault(p => p.ProductId == id);

            if (item != null)
            {
                wishlist.Remove(item);
                HttpContext.Session.SetObject("Wishlist", wishlist);
                TempData["Success"] = "Đã xóa khỏi mục yêu thích!";
            }

            return RedirectToAction("Index");
        }

        // ================= XÓA TOÀN BỘ =================
        public IActionResult Clear()
        {
            HttpContext.Session.Remove("Wishlist");
            TempData["Success"] = "Đã làm trống danh mục yêu thích!";
            return RedirectToAction("Index");
        }
    }
}
