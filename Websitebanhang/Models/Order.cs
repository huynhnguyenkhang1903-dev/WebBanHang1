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
        public string PaymentMethod { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = "Pending";
        public List<CartItem>? Items { get; set; }
    }
}