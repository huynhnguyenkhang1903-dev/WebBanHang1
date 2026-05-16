using Websitebanhang.Models;

namespace Websitebanhang.Data
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            // Seed 5 categories
            var categoryNames = new[] 
            { 
                "Cà phê hạt", 
                "Cà phê pha sẵn", 
                "Quà tặng & Bộ quà tặng", 
                "Nguyên liệu pha chế", 
                "Dụng cụ pha chế" 
            };

            foreach (var cName in categoryNames)
            {
                if (!context.Categories.Any(c => c.Name == cName))
                {
                    context.Categories.Add(new Category { Name = cName });
                }
            }
            context.SaveChanges();

            // Seed Units of Measure
            var unitNames = new[] { "Kg", "Lít", "Ml", "Gói", "Hộp", "Ly", "Cái", "Bộ" };
            foreach (var uName in unitNames)
            {
                if (!context.UnitsOfMeasure.Any(u => u.Name == uName))
                {
                    context.UnitsOfMeasure.Add(new UnitOfMeasure { Name = uName, Description = "Đơn vị tính " + uName });
                }
            }
            context.SaveChanges();

            // Xóa các danh mục cũ (Máy pha cà phê, Linh kiện)
            var oldCategories = context.Categories
                .Where(c => c.Name == "Máy pha cà phê" || c.Name == "Linh kiện")
                .ToList();

            if (oldCategories.Any())
            {
                // Kiểm tra xem có sản phẩm nào thuộc danh mục này không, nếu có chuyển về danh mục mặc định
                var defaultCat = context.Categories.FirstOrDefault(c => c.Name == "Cà phê hạt");
                var defaultCatId = defaultCat != null ? defaultCat.Id : 1;

                foreach (var oldCat in oldCategories)
                {
                    var productsToUpdate = context.Products.Where(p => p.CategoryId == oldCat.Id).ToList();
                    foreach (var p in productsToUpdate)
                    {
                        p.CategoryId = defaultCatId;
                    }
                }
                context.SaveChanges();

                context.Categories.RemoveRange(oldCategories);
                context.SaveChanges();
            }

            if (context.Products.Any())
                return;

            var countries = new[]
            {
                "Vietnam", "Brazil", "Colombia", "Ethiopia", "Indonesia"
            };

            var firstCat = context.Categories.FirstOrDefault(c => c.Name == "Cà phê hạt");
            int catId = firstCat != null ? firstCat.Id : 1;

            var products = new List<Product>();

            for (int i = 1; i <= 70; i++)
            {
                products.Add(new Product
                {
                    Name = "Coffee Product " + i,
                    Price = 50000 + (i * 10000),
                    Description = "Delicious coffee number " + i,
                    Country = countries[i % countries.Length],
                    CategoryId = catId,
                    ImageUrl = "/images/coffee.jpg"
                });
            }

            context.Products.AddRange(products);
            context.SaveChanges();

            // Update old orders
            var paidOrders = context.Orders.Where(o => o.Status == "Paid").ToList();
            if (paidOrders.Any())
            {
                foreach (var order in paidOrders)
                {
                    order.Status = "Completed";
                    order.IsPaid = true;
                }
                context.SaveChanges();
            }
        }
    }
}