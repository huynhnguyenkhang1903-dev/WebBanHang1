using System;
using System.Collections.Generic;

namespace Websitebanhang.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";

        public string PaymentMethod { get; set; } = "COD";

        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }

        public string Status { get; set; } = "Pending";

        public bool IsPaid { get; set; } = false;
        public string? TransactionId { get; set; }
        public DateTime? PaidAt { get; set; }

        public string? PaymentContent { get; set; }

        public List<CartItem>? Items { get; set; }
    }
}