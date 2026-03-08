using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Models;
using Websitebanhang.Repositores;
using System.Text.Json;

public class CartController : Controller
{
    private readonly IProductRepository _productRepository;

    public CartController(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    // ================= VIEW CART =================

    public IActionResult Index()
    {
        var cart = GetCart();
        return View(cart);
    }

    // ================= ADD TO CART =================

    public IActionResult Add(int id)
    {
        var product = _productRepository.GetById(id);

        var cart = GetCart();

        var item = cart.FirstOrDefault(p => p.Product!.Id == id);

        if (item == null)
        {
            cart.Add(new CartItem
            {
                Product = product,
                Quantity = 1
            });
        }
        else
        {
            item.Quantity++;
        }

        SaveCart(cart);

        return RedirectToAction("Index");
    }

    // ================= REMOVE PRODUCT =================

    public IActionResult Remove(int id)
    {
        var cart = GetCart();

        var item = cart.FirstOrDefault(p => p.Product!.Id == id);

        if (item != null)
        {
            cart.Remove(item);
        }

        SaveCart(cart);

        return RedirectToAction("Index");
    }

    // ================= INCREASE QUANTITY =================

    public IActionResult Increase(int id)
    {
        var cart = GetCart();

        var item = cart.FirstOrDefault(p => p.Product!.Id == id);

        if (item != null)
        {
            item.Quantity++;
        }

        SaveCart(cart);

        return RedirectToAction("Index");
    }

    // ================= DECREASE QUANTITY =================

    public IActionResult Decrease(int id)
    {
        var cart = GetCart();

        var item = cart.FirstOrDefault(p => p.Product!.Id == id);

        if (item != null)
        {
            item.Quantity--;

            if (item.Quantity <= 0)
                cart.Remove(item);
        }

        SaveCart(cart);

        return RedirectToAction("Index");
    }

    // ================= CLEAR CART =================

    public IActionResult Clear()
    {
        HttpContext.Session.Remove("Cart");

        return RedirectToAction("Index");
    }

    // ================= GET CART =================

    private List<CartItem> GetCart()
    {
        var cartJson = HttpContext.Session.GetString("Cart");

        if (cartJson == null)
            return new List<CartItem>();

        return JsonSerializer.Deserialize<List<CartItem>>(cartJson)!;
    }

    // ================= SAVE CART =================

    private void SaveCart(List<CartItem> cart)
    {
        HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
    }
}