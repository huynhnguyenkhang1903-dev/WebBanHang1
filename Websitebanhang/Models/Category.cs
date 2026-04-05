using System;
using System.Collections.Generic;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Country { get; set; } = string.Empty;

    public string ImageUrl { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public int? VoucherId { get; set; }

    public Category Category { get; set; }

    public Voucher? Voucher { get; set; }
}

public class Voucher
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public int DiscountPercent { get; set; }

    public DateTime ExpiryDate { get; set; }
}