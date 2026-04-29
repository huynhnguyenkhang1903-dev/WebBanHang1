using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
{
    public class WebsiteSetting
    {
        [Key]
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string? Description { get; set; }
    }
}
