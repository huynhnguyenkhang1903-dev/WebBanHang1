using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class UnitOfMeasure
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên đơn vị tích không được để trống")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }
    }
}
