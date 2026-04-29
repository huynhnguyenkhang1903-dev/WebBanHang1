using System;
using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class StockHistory
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required]
        public int QuantityChange { get; set; } // +10 for stock in, -5 for stock out

        public int BalanceAfter { get; set; } // Current stock after change

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "Manual"; // Manual, Sale, Cancelled, Returned

        [StringLength(200)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
