using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá không được để trống")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        // Quốc gia sản phẩm
        public string Country { get; set; } = "Vietnam";

        [Required(ErrorMessage = "Số lượng tồn kho không được để trống")]
        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không hợp lệ")]
        public int Stock { get; set; } = 0;

        // Category
        [Required]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        // Ảnh đại diện
        public string? ImageUrl { get; set; }

        public int? VoucherId { get; set; }

        public Voucher? Voucher { get; set; }

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}