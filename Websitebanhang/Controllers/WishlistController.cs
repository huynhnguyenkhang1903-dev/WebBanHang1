using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Websitebanhang.Data;
using Websitebanhang.Helpers;
using Websitebanhang.Models;
using Websitebanhang.Repositores;

namespace Websitebanhang.Controllers
{
    public class WishlistController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(
            IProductRepository productRepository,
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
        }

        // ================= XEM DANH MỤC YÊU THÍCH =================
        public async Task<IActionResult> Index()
        {
            var wishlist = new List<CartItem>();

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var dbItems = await _context.WishlistItems
                        .Include(w => w.Product)
                        .Where(w => w.UserId == user.Id)
                        .ToListAsync();

                    foreach (var item in dbItems)
                    {
                        if (item.Product != null)
                        {
                            wishlist.Add(new CartItem
                            {
                                ProductId = item.ProductId,
                                Name = item.Product.Name ?? "",
                                Price = item.Product.Price,
                                Quantity = 1,
                                ImageUrl = item.Product.ImageUrl ?? ""
                            });
                        }
                    }
                }
            }
            else
            {
                wishlist = HttpContext.Session.GetObject<List<CartItem>>("Wishlist") ?? new List<CartItem>();
            }

            return View(wishlist);
        }

        // ================= THÊM VÀO YÊU THÍCH =================
        public async Task<IActionResult> Add(int id, string returnUrl)
        {
            var product = _productRepository.GetById(id);
            if (product == null) return NotFound();

            bool isAdded = false;

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var exists = await _context.WishlistItems.AnyAsync(w => w.UserId == user.Id && w.ProductId == id);
                    if (!exists)
                    {
                        _context.WishlistItems.Add(new WishlistItem
                        {
                            UserId = user.Id,
                            ProductId = id,
                            AddedAt = DateTime.Now
                        });
                        await _context.SaveChangesAsync();
                        isAdded = true;
                    }
                }
            }
            else
            {
                var wishlist = HttpContext.Session.GetObject<List<CartItem>>("Wishlist") ?? new List<CartItem>();
                var item = wishlist.FirstOrDefault(p => p.ProductId == id);

                if (item == null)
                {
                    wishlist.Add(new CartItem
                    {
                        ProductId = product.Id,
                        Name = product.Name ?? "",
                        Price = product.Price,
                        Quantity = 1,
                        ImageUrl = product.ImageUrl ?? ""
                    });
                    HttpContext.Session.SetObject("Wishlist", wishlist);
                    isAdded = true;
                }
            }

            if (isAdded)
            {
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
        public async Task<IActionResult> Remove(int id)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var item = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == user.Id && w.ProductId == id);
                    if (item != null)
                    {
                        _context.WishlistItems.Remove(item);
                        await _context.SaveChangesAsync();
                        TempData["Success"] = "Đã xóa khỏi mục yêu thích!";
                    }
                }
            }
            else
            {
                var wishlist = HttpContext.Session.GetObject<List<CartItem>>("Wishlist") ?? new List<CartItem>();
                var item = wishlist.FirstOrDefault(p => p.ProductId == id);

                if (item != null)
                {
                    wishlist.Remove(item);
                    HttpContext.Session.SetObject("Wishlist", wishlist);
                    TempData["Success"] = "Đã xóa khỏi mục yêu thích!";
                }
            }

            return RedirectToAction("Index");
        }

        // ================= XÓA TOÀN BỘ =================
        public async Task<IActionResult> Clear()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    var items = await _context.WishlistItems.Where(w => w.UserId == user.Id).ToListAsync();
                    if (items.Any())
                    {
                        _context.WishlistItems.RemoveRange(items);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            else
            {
                HttpContext.Session.Remove("Wishlist");
            }
            
            TempData["Success"] = "Đã làm trống danh mục yêu thích!";
            return RedirectToAction("Index");
        }
    }
}
