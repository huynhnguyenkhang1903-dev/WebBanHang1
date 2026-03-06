using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public decimal Price { get; set; }

        public string? Description { get; set; }

        public int CategoryId { get; set; }

        // Hình đại diện
        public string? ImageUrl { get; set; }

        // Danh sách hình
        public List<string>? ImageUrls { get; set; }
    }
}