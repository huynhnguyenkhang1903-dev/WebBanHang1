using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Data;
using Websitebanhang.Models;
using Websitebanhang.Helpers;

public class CheckoutController : Controller
{
    private readonly AppDbContext _context;

    public CheckoutController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Checkout()
    {
        var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart");

        if (cart == null || !cart.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        decimal total = cart.Sum(x => x.Price * x.Quantity);

        var order = new Order
        {
            CustomerName = "Test User",
            Email = "test@gmail.com",
            Phone = "0123456789",
            Address = "Tây Ninh",
            PaymentMethod = "BANK",
            TotalAmount = total,
            OrderDate = DateTime.Now,
            Status = "Pending",
            Items = cart
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // 🔥 tạo nội dung chuyển khoản
        order.PaymentContent = $"ORDER{order.Id}";
        await _context.SaveChangesAsync();

        // 🔥 tạo QR
        string qrUrl = $"https://img.vietqr.io/image/970422-123456789-compact.png" +
                       $"?amount={order.TotalAmount}" +
                       $"&addInfo={order.PaymentContent}" +
                       $"&accountName=NGUYEN VAN A";

        ViewBag.QRCode = qrUrl;
        ViewBag.Amount = order.TotalAmount;
        ViewBag.Content = order.PaymentContent;
        ViewBag.OrderId = order.Id;

        return View("BankPayment");
    }
}