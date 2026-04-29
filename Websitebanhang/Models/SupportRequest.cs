using System;
using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class SupportRequest
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chủ đề")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung yêu cầu")]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string Status { get; set; } = "Open"; // Open, Processing, Resolved, Closed

        public string? AdminNote { get; set; }

        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }
    }
}
