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
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Promotion> Promotions { get; set; }

        public DbSet<UnitOfMeasure> UnitsOfMeasure { get; set; }

        public DbSet<Voucher> Voucher { get; set; } // Add Voucher DbSet

        public DbSet<UserAddress> UserAddresses { get; set; }

        public DbSet<Banner> Banners { get; set; }
        
        public DbSet<Review> Reviews { get; set; }

        public DbSet<WishlistItem> WishlistItems { get; set; }
        public DbSet<StockHistory> StockHistories { get; set; }
        public DbSet<ProductViewHistory> ProductViewHistories { get; set; }
        public DbSet<SupportRequest> SupportRequests { get; set; }
        public DbSet<RewardPointHistory> RewardPointHistories { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<AdminActivityLog> AdminActivityLogs { get; set; }
        public DbSet<WebsiteSetting> WebsiteSettings { get; set; }

        // CONFIG DATABASE
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Supplier - Product Relationship
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);

            // Fix decimal warning for Price
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(o => o.ShippingCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CartItem>()
                .Property(ci => ci.Price)
                .HasPrecision(18, 2);
        }
    }
}