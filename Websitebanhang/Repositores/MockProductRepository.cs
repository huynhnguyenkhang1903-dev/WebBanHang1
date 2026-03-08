using System.Collections.Generic;
using System.Linq;
using Websitebanhang.Models;
<<<<<<< HEAD

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
                new Product { Id = 10, Name = "Cà phê Sữa Đá Blend", Price = 95000, Description = "Đậm đà khi pha sữa." },

                new Product { Id = 11, Name = "Cà phê Honey Process", Price = 160000, Description = "Chế biến mật ong." },
                new Product { Id = 12, Name = "Cà phê Natural Process", Price = 155000, Description = "Chế biến tự nhiên." },
                new Product { Id = 13, Name = "Cà phê Washed Process", Price = 150000, Description = "Chế biến ướt." },
                new Product { Id = 14, Name = "Cà phê Ethiopia", Price = 180000, Description = "Hương hoa quả đặc trưng." },
                new Product { Id = 15, Name = "Cà phê Colombia", Price = 175000, Description = "Hương vị cân bằng." },
                new Product { Id = 16, Name = "Cà phê Brazil Santos", Price = 165000, Description = "Vị chocolate nhẹ." },
                new Product { Id = 17, Name = "Cà phê Guatemala", Price = 170000, Description = "Hương cacao." },
                new Product { Id = 18, Name = "Cà phê Kenya AA", Price = 185000, Description = "Chua thanh mạnh." },
                new Product { Id = 19, Name = "Cà phê Indonesia Mandheling", Price = 175000, Description = "Đậm và ít chua." },
                new Product { Id = 20, Name = "Cà phê Peru Organic", Price = 190000, Description = "Cà phê hữu cơ." },

                new Product { Id = 21, Name = "Cà phê Rang Mộc", Price = 100000, Description = "Không phụ gia." },
                new Product { Id = 22, Name = "Cà phê Rang Vừa", Price = 105000, Description = "Hương vị cân bằng." },
                new Product { Id = 23, Name = "Cà phê Rang Đậm", Price = 110000, Description = "Đậm vị truyền thống." },
                new Product { Id = 24, Name = "Cà phê Xay Phin", Price = 95000, Description = "Xay sẵn cho phin." },
                new Product { Id = 25, Name = "Cà phê Xay Máy", Price = 115000, Description = "Xay cho máy Espresso." },
                new Product { Id = 26, Name = "Cà phê Hạt Nguyên Chất", Price = 120000, Description = "Hạt nguyên chưa xay." },
                new Product { Id = 27, Name = "Cà phê Blend House", Price = 130000, Description = "Công thức riêng của quán." },
                new Product { Id = 28, Name = "Cà phê Blend Premium", Price = 150000, Description = "Blend cao cấp." },
                new Product { Id = 29, Name = "Cà phê Specialty", Price = 200000, Description = "Cà phê đặc sản." },
                new Product { Id = 30, Name = "Cà phê Single Origin", Price = 180000, Description = "Nguồn gốc đơn." },

                new Product { Id = 31, Name = "Cà phê Đặc Biệt Đà Lạt", Price = 170000, Description = "Cà phê cao cấp Đà Lạt." },
                new Product { Id = 32, Name = "Cà phê Cao Cấp Buôn Ma Thuột", Price = 165000, Description = "Đậm vị Tây Nguyên." },
                new Product { Id = 33, Name = "Cà phê Gourmet Blend", Price = 190000, Description = "Blend dành cho quán." },
                new Product { Id = 34, Name = "Cà phê Premium Arabica", Price = 210000, Description = "Arabica cao cấp." },
                new Product { Id = 35, Name = "Cà phê Signature House", Price = 220000, Description = "Cà phê đặc trưng của thương hiệu." }
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
=======
public class MockProductRepository : IProductRepository
{
    private readonly List<Product> _products;
    public MockProductRepository()
    {
        // Tạo một số dữ liệu mẫu
        _products = new List<Product>
{
new Product { Id = 1, Name = "Laptop", Price = 1000,
Description = "A high-end laptop"},
// Thêm các sản phẩm khác
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
        product.Id = _products.Max(p => p.Id) + 1;
    
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
>>>>>>> ee325eaf63f2aabb046ebc4c33770f92d4a56eca
