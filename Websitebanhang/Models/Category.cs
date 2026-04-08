using System;
using System.Collections.Generic;

namespace Websitebanhang.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class Voucher
    {
        public int Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public int DiscountPercent { get; set; }

        public DateTime ExpiryDate { get; set; }
    }
}