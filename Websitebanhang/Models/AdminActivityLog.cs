using System;
using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class AdminActivityLog
    {
        public int Id { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // e.g. "Create", "Update", "Delete", "ChangeStatus"

        [Required]
        public string EntityType { get; set; } = string.Empty; // e.g. "Product", "Order", "Supplier"

        public string? EntityId { get; set; }

        public string? Description { get; set; } // e.g. "Updated product price from 100 to 120"

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public string? IpAddress { get; set; }
    }
}
