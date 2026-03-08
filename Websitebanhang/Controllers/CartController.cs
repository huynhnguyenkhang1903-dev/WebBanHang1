using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Models;
using Websitebanhang.Repositores;
using Websitebanhang.Extensions;

namespace Websitebanhang.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;

        public CartController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // hiển thị giỏ hàng
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            return View(cart);
        }

        // thêm vào giỏ hàng
        public IActionResult AddToCart(int id)
        {
            var product = _productRepository.GetById(id);

            if (product == null)
                return NotFound();

            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item != null)
            {
                item.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name ?? "",
                    Price = product.Price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl ?? ""
                });
            }

            HttpContext.Session.SetObject("Cart", cart);

            return RedirectToAction("Index");
        }

        // xoá sản phẩm
        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item != null)
            {
                cart.Remove(item);
            }

            HttpContext.Session.SetObject("Cart", cart);

            return RedirectToAction("Index");
        }

        // cập nhật số lượng
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

            var item = cart.FirstOrDefault(p => p.ProductId == productId);

            if (item != null)
            {
                item.Quantity = quantity;
            }

            HttpContext.Session.SetObject("Cart", cart);

            return RedirectToAction("Index");
        }

        // chọn sản phẩm để checkout
        [HttpPost]
        public IActionResult Checkout(List<int> selectedProducts)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

            var selectedItems = cart
                .Where(p => selectedProducts.Contains(p.ProductId))
                .ToList();

            return View(selectedItems);
        }

        // đặt hàng
        [HttpPost]
        public IActionResult PlaceOrder(string name, string address, string phone)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (cart.Count == 0)
            {
                return RedirectToAction("Index");
            }

            // tính tổng tiền
            decimal total = cart.Sum(p => p.Price * p.Quantity);

            // truyền dữ liệu sang trang success
            ViewBag.CustomerName = name;
            ViewBag.Address = address;
            ViewBag.Phone = phone;
            ViewBag.Total = total;

            // xoá giỏ hàng
            HttpContext.Session.Remove("Cart");

            return View("OrderSuccess");
        }

        // trang đặt hàng thành công
        public IActionResult OrderSuccess()
        {
            return View();
        }
    }
}