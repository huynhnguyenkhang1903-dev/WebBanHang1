using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class ProductAttribute
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required(ErrorMessage = "Tên thuộc tính không được để trống")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Giá trị thuộc tính không được để trống")]
        [StringLength(255)]
        public string Value { get; set; } = string.Empty;
    }
}
