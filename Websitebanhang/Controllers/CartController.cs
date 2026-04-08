using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Models;
using Websitebanhang.Repositores;
using Websitebanhang.Extensions;
using Websitebanhang.Services;

namespace Websitebanhang.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly IEmailService _emailService;

        public CartController(IProductRepository productRepository, IEmailService emailService)
        {
            _productRepository = productRepository;
            _emailService = emailService;
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
        public async Task<IActionResult> PlaceOrder(string name, string email, string address, string phone, string paymentMethod)
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
            ViewBag.Email = email;
            ViewBag.Address = address;
            ViewBag.Phone = phone;
            var formattedPayment = paymentMethod == "cod" ? "Thanh toán khi nhận hàng" : "Chuyển khoản ngân hàng";
            ViewBag.PaymentMethod = formattedPayment;
            ViewBag.Total = total;

            // Send confirmation email
            if (!string.IsNullOrWhiteSpace(email))
            {
                string htmlBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                        <h2 style='color: #4CAF50; text-align: center;'>Thanh toán thành công!</h2>
                        <hr />
                        <h3>Xin chào <strong>{name}</strong>,</h3>
                        <p>Cảm ơn bạn đã mua hàng tại <strong>Coffee Shop</strong>. Đơn hàng của bạn đã được ghi nhận và đang trong quá trình xử lý.</p>
                        
                        <h4>Chi tiết giao hàng:</h4>
                        <ul>
                            <li><strong>SĐT:</strong> {phone}</li>
                            <li><strong>Địa chỉ:</strong> {address}</li>
                            <li><strong>Phương thức:</strong> {formattedPayment}</li>
                        </ul>

                        <h3 style='background: #f4f4f4; padding: 10px; border-radius: 5px; text-align: center;'>
                            Tổng tiền: <span style='color: #D32F2F;'>{total.ToString("N0")} ₫</span>
                        </h3>
                        
                        <p style='text-align: center; color: #777; margin-top: 20px;'>
                            Nếu bạn có bất kỳ câu hỏi nào, vui lòng trả lời email này.
                        </p>
                    </div>
                ";

                try 
                {
                    await _emailService.SendEmailAsync(email, "Xác nhận đơn hàng - Coffee Shop", htmlBody);
                } 
                catch (Exception)
                {
                    // Lỗi gửi email không nên làm crash tiến trình đặt hàng
                }
            }

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