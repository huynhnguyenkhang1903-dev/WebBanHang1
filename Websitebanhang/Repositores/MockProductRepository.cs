using System.Collections.Generic;
using System.Linq;
using Websitebanhang.Models;

namespace Websitebanhang.Repositores
{
    public class MockProductRepository : IProductRepository
    {
        private readonly List<Product> _products;

        public MockProductRepository()
        {
            _products = new List<Product>
            {
                new Product { Id = 1, Name = "Cà phê Arabica Đà Lạt", Price = 120000, Description = "Hương thơm nhẹ, vị chua thanh." },
                new Product { Id = 2, Name = "Cà phê Robusta Buôn Ma Thuột", Price = 90000, Description = "Đậm vị, caffeine cao." },
                new Product { Id = 3, Name = "Cà phê Culi Robusta", Price = 110000, Description = "Hạt tròn, vị đậm mạnh." },
                new Product { Id = 4, Name = "Cà phê Moka Cầu Đất", Price = 150000, Description = "Hương thơm quyến rũ." },
                new Product { Id = 5, Name = "Cà phê Espresso Blend", Price = 130000, Description = "Phù hợp pha máy Espresso." },
                new Product { Id = 6, Name = "Cà phê Cappuccino Blend", Price = 140000, Description = "Vị cân bằng, béo nhẹ." },
                new Product { Id = 7, Name = "Cà phê Latte Blend", Price = 135000, Description = "Phù hợp pha Latte." },
                new Product { Id = 8, Name = "Cà phê Cold Brew", Price = 125000, Description = "Thích hợp pha lạnh." },
                new Product { Id = 9, Name = "Cà phê Phin Truyền Thống", Price = 85000, Description = "Hương vị cà phê Việt Nam." },
                new Product { Id = 10, Name = "Cà phê Sữa Đá Blend", Price = 95000, Description = "Đậm đà khi pha sữa." }
            };
        }

        public IEnumerable<Product> GetAll()
        {
            return _products;
        }

        public Product? GetById(int id)
        {
            return _products.FirstOrDefault(p => p.Id == id);
        }

        public void Add(Product product)
        {
            if (_products.Any())
                product.Id = _products.Max(p => p.Id) + 1;
            else
                product.Id = 1;

            _products.Add(product);
        }

        public void Update(Product product)
        {
            var index = _products.FindIndex(p => p.Id == product.Id);
            if (index != -1)
            {
                _products[index] = product;
            }
        }

        public void Delete(int id)
        {
            var product = _products.FirstOrDefault(p => p.Id == id);
            if (product != null)
            {
                _products.Remove(product);
            }
        }
    }
}