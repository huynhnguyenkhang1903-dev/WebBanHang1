using Websitebanhang.Models;

namespace Websitebanhang.Data
{
    public static class DbInitializer
    {
        public static void Seed(AppDbContext context)
        {
            if (context.Products.Any())
                return;

            var countries = new[]
            {
                "Vietnam", "Brazil", "Colombia", "Ethiopia", "Indonesia"
            };

            var products = new List<Product>();

            for (int i = 1; i <= 70; i++)
            {
                products.Add(new Product
                {
                    Name = "Coffee Product " + i,
                    Price = 50000 + (i * 10000),
                    Description = "Delicious coffee number " + i,
                    Country = countries[i % countries.Length],
                    CategoryId = 1,
                    ImageUrl = "/images/coffee.jpg"
                });
            }

            context.Products.AddRange(products);
            context.SaveChanges();
        }
    }
}