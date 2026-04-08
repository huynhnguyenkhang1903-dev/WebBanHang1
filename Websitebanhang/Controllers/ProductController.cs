using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Websitebanhang.Repositores;
using X.PagedList;
using X.PagedList.Extensions;


// 🔥 FIX namespace Product
using ProductModel = Websitebanhang.Models.Product;

namespace Websitebanhang.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;

        public ProductController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        [AllowAnonymous]
        public IActionResult Index(string search, string price, string country, int page = 1)
        {
            var products = _productRepository.GetAll().AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products.Where(p =>
                    p.Name != null &&
                    p.Name.ToLower().Contains(search.ToLower()));
            }

            // FILTER PRICE
            if (!string.IsNullOrWhiteSpace(price))
            {
                switch (price)
                {
                    case "low":
                        products = products.Where(p => p.Price < 100000);
                        break;

                    case "medium":
                        products = products.Where(p =>
                            p.Price >= 100000 && p.Price <= 300000);
                        break;

                    case "high":
                        products = products.Where(p => p.Price > 300000);
                        break;
                }
            }

            // FILTER COUNTRY
            if (!string.IsNullOrWhiteSpace(country))
            {
                products = products.Where(p =>
                    p.Country != null &&
                    p.Country == country);
            }

            int pageSize = 8;

            // 🔥 FIX QUAN TRỌNG
            var pagedProducts = products
    .OrderBy(p => p.Id)
    .ToList()
    .Select(p => new ProductModel
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price,
        Country = p.Country,
        ImageUrl = p.ImageUrl
    })
    .ToPagedList(page, pageSize);

            return View(pagedProducts);
        }

        [AllowAnonymous]
        public IActionResult Display(int id)
        {
            var product = _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }
    }
}