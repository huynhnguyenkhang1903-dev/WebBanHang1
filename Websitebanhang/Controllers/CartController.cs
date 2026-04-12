using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Models;
using Websitebanhang.Repositores;
using Websitebanhang.Extensions;
using Websitebanhang.Services;
using Microsoft.AspNetCore.Authorization;

namespace Websitebanhang.Controllers
{
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly IEmailService _emailService;
        private readonly Websitebanhang.Data.AppDbContext _context;

        public CartController(IProductRepository productRepository, IEmailService emailService, Websitebanhang.Data.AppDbContext context)
        {
            _productRepository = productRepository;
            _emailService = emailService;
            _context = context;
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
        [Authorize]
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

            // Create items list HTML
            var itemsHtml = string.Join("", cart.Select(item => $@"
                <tr>
                    <td style='padding: 10px; border-bottom: 1px solid #eee;'>{item.Name}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 10px; border-bottom: 1px solid #eee; text-align: right;'>{(item.Price * item.Quantity).ToString("N0")} ₫</td>
                </tr>
            "));

            // Send confirmation email
            if (!string.IsNullOrWhiteSpace(email))
            {
                string htmlBody = $@"
                    <div style='font-family: ""Helvetica Neue"", Helvetica, Arial, sans-serif; max-width: 650px; margin: auto; padding: 30px; border: 1px solid #eaeaea; border-radius: 12px; background-color: #ffffff; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
                        
                        <div style='text-align: center; margin-bottom: 25px;'>
                            <h1 style='color: #6F4E37; margin: 0;'>☕ Coffee Shop</h1>
                            <p style='color: #777; font-size: 14px; margin-top: 5px;'>Fresh Beans • Quality Coffee</p>
                        </div>

                        <div style='background-color: #e8f5e9; padding: 15px; border-radius: 8px; text-align: center; margin-bottom: 25px;'>
                            <h2 style='color: #2e7d32; margin: 0;'>Xác Nhận Đơn Hàng Thành Công!</h2>
                        </div>
                        
                        <h3 style='color: #333;'>Xin chào <strong>{name}</strong>,</h3>
                        <p style='color: #555; line-height: 1.6;'>Cảm ơn bạn đã mua sắm tại <strong>Coffee Shop</strong>. Đơn hàng của bạn đã được ghi nhận thành công và đang được chúng tôi xử lý. Dưới đây là thông tin chi tiết đơn hàng của bạn:</p>
                        
                        <table style='width: 100%; border-collapse: collapse; margin-top: 20px; margin-bottom: 25px;'>
                            <thead>
                                <tr style='background-color: #f9f9f9;'>
                                    <th style='padding: 12px 10px; text-align: left; border-bottom: 2px solid #ddd; color: #333;'>Sản phẩm</th>
                                    <th style='padding: 12px 10px; text-align: center; border-bottom: 2px solid #ddd; color: #333;'>Số lượng</th>
                                    <th style='padding: 12px 10px; text-align: right; border-bottom: 2px solid #ddd; color: #333;'>Thành tiền</th>
                                </tr>
                            </thead>
                            <tbody>
                                {itemsHtml}
                            </tbody>
                            <tfoot>
                                <tr>
                                    <td colspan='2' style='padding: 15px 10px; text-align: right; font-weight: bold; font-size: 16px;'>Tổng Tiền:</td>
                                    <td style='padding: 15px 10px; text-align: right; font-weight: bold; font-size: 18px; color: #D32F2F;'>
                                        {total.ToString("N0")} ₫
                                    </td>
                                </tr>
                            </tfoot>
                        </table>

                        <h4 style='color: #333; margin-top: 0;'>Chi tiết giao hàng:</h4>
                        <ul style='color: #555; line-height: 1.8; list-style-type: none; padding-left: 0; background: #fafafa; padding: 15px; border-radius: 8px;'>
                            <li><strong>📞 SĐT:</strong> {phone}</li>
                            <li><strong>📍 Địa chỉ:</strong> {address}</li>
                            <li><strong>💳 Phương thức thanh toán:</strong> {formattedPayment}</li>
                        </ul>
                        
                        <p style='text-align: center; color: #999; font-size: 13px; margin-top: 30px; border-top: 1px solid #eee; padding-top: 20px;'>
                            Đây là email tự động, vui lòng không trả lời. Nếu bạn cần hỗ trợ, hãy liên hệ qua hotline hoặc email support@coffeeshop.com.
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

            // LƯU ĐƠN HÀNG VÀO DATABASE
            var order = new Order
            {
                CustomerName = name,
                Email = email,
                Address = address,
                Phone = phone,
                PaymentMethod = formattedPayment,
                TotalAmount = total,
                OrderDate = DateTime.Now,
                Status = "Pending",
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