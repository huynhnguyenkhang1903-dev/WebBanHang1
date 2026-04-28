using System;
using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung bình luận")]
        [StringLength(1000, ErrorMessage = "Bình luận không được quá 1000 ký tự")]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
