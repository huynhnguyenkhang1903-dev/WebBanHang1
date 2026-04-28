using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Websitebanhang.Models;

namespace Websitebanhang.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // TABLES
        public DbSet<Product> Products { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Promotion> Promotions { get; set; }

        public DbSet<UnitOfMeasure> UnitsOfMeasure { get; set; }

        public DbSet<Voucher> Voucher { get; set; } // Add Voucher DbSet

        public DbSet<UserAddress> UserAddresses { get; set; }

        public DbSet<Banner> Banners { get; set; }

        // CONFIG DATABASE
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fix decimal warning for Price
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CartItem>()
                .Property(ci => ci.Price)
                .HasPrecision(18, 2);
        }
    }
}