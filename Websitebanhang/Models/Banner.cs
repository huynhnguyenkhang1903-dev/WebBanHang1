using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class Banner
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập link ảnh")]
        [Display(Name = "Đường dẫn ảnh")]
        public string ImageUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
        [Display(Name = "Tiêu đề chính")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Tiêu đề phụ / Mô tả")]
        public string Subtitle { get; set; } = string.Empty;

        [Display(Name = "Sắp xếp")]
        public int OrderIndex { get; set; } = 0;

        [Display(Name = "Hiển thị")]
        public bool IsActive { get; set; } = true;
    }
}
