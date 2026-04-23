using System;
using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class Promotion
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [Range(0, 100)]
        public int DiscountPercent { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
    }
}