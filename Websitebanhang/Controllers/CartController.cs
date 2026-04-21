using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Models;
using Websitebanhang.Repositores;
using Websitebanhang.Helpers;
using Websitebanhang.Services;
using Microsoft.AspNetCore.Authorization;

namespace Websitebanhang.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly IEmailService _emailService;
        private readonly Websitebanhang.Data.AppDbContext _context;

        public CartController(
            IProductRepository productRepository,
            IEmailService emailService,
            Websitebanhang.Data.AppDbContext context)
        {
            _productRepository = productRepository;
            _emailService = emailService;
            _context = context;
        }

        // ================= GIỎ HÀNG =================
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            return View(cart);
        }

        public IActionResult AddToCart(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item != null)
                item.Quantity++;
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

        public IActionResult Remove(int id)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(p => p.ProductId == id);

            if (item != null)
                cart.Remove(item);

            HttpContext.Session.SetObject("Cart", cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(p => p.ProductId == productId);

            if (item != null)
                item.Quantity = quantity;

            HttpContext.Session.SetObject("Cart", cart);
            return RedirectToAction("Index");
        }

        // ================= CHECKOUT =================
        [HttpPost]
        public IActionResult Checkout(List<int> selectedProducts)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

            var selectedItems = cart
                .Where(p => selectedProducts.Contains(p.ProductId))
                .ToList();

            return View(selectedItems);
        }

        // ================= ĐẶT HÀNG =================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PlaceOrder(string name, string email, string address, string phone, string paymentMethod)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (cart.Count == 0) return RedirectToAction("Index");

            decimal total = cart.Sum(p => p.Price * p.Quantity);

            var order = new Order
            {
                CustomerName = name,
                Email = email,
                Address = address,
                Phone = phone,
                PaymentMethod = paymentMethod,
                TotalAmount = total,
                OrderDate = DateTime.Now,
                Status = "Pending",
                IsPaid = false,
                Items = cart.Select(c => new CartItem
                {
                    ProductId = c.ProductId,
                    Name = c.Name,
                    Price = c.Price,
                    Quantity = c.Quantity,
                    ImageUrl = c.ImageUrl
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            order.PaymentContent = $"ORDER{order.Id}";
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");

            // 🔥 BANK
            if (paymentMethod == "bank")
            {
                string bankCode = "970422";
                string accountNumber = "0344506553";
                string accountName = "HUYNH NGUYEN KHANG";

                string qrUrl = $"https://img.vietqr.io/image/{bankCode}-{accountNumber}-compact.png" +
                               $"?amount={order.TotalAmount}" +
                               $"&addInfo={order.PaymentContent}" +
                               $"&accountName={accountName}";

                ViewBag.QRCode = qrUrl;
                ViewBag.Amount = order.TotalAmount;
                ViewBag.Content = order.PaymentContent;
                ViewBag.OrderId = order.Id;

                return View("BankPayment");
            }

            return View("OrderSuccess");
        }

        // ================= HỦY ĐƠN =================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            if (order.IsPaid)
            {
                TempData["Error"] = "Đơn đã thanh toán, không thể huỷ!";
                return RedirectToAction("Profile", "Account");
            }

            order.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Huỷ đơn thành công!";
            return RedirectToAction("Profile", "Account");
        }

        // ================= CHECK PAYMENT =================
        public async Task<IActionResult> CheckPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            // 🔥 CHẶN nếu đã huỷ
            if (order.Status == "Cancelled")
                return BadRequest("Đơn đã bị huỷ");

            order.IsPaid = true;
            order.Status = "Paid";
            order.PaidAt = DateTime.Now;
            order.TransactionId = Guid.NewGuid().ToString();

            await _context.SaveChangesAsync();

            return RedirectToAction("PaymentSuccess", new { id = orderId });
        }

        // ================= SUCCESS =================
        public IActionResult OrderSuccess()
        {
            return View();
        }

        public IActionResult PaymentSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }
    }
}