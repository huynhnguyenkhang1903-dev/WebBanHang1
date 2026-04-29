using System;
using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class RewardPointHistory
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public int PointsChanged { get; set; } // + for earning, - for spending
        public int BalanceAfter { get; set; }
        
        [StringLength(200)]
        public string Note { get; set; } = string.Empty; // e.g. "Earned from order #123"
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
