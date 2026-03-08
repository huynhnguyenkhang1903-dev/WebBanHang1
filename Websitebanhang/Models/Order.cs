using System.Collections.Generic;

namespace Websitebanhang.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = "";

        public string Address { get; set; } = "";

        public string Phone { get; set; } = "";

        public List<CartItem>? Items { get; set; }
    }
}