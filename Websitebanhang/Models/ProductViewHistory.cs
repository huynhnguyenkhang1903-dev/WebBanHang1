using System;
using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class ProductViewHistory
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.Now;
    }
}
