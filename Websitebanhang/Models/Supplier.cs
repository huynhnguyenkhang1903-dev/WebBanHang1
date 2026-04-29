using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên nhà cung cấp không được để trống")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(20)]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        public string? Note { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
