<<<<<<< HEAD
﻿using System.Collections.Generic;
=======
﻿using System.ComponentModel.DataAnnotations;
>>>>>>> ee325eaf63f2aabb046ebc4c33770f92d4a56eca

namespace Websitebanhang.Models
{
    public class Product
    {
        public int Id { get; set; }

<<<<<<< HEAD
        public string Name { get; set; } = "";

        public decimal Price { get; set; }

        public string Description { get; set; } = "";

        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public string? ImageUrl { get; set; }

=======
        [Required]
        public string? Name { get; set; }

        public decimal Price { get; set; }

        public string? Description { get; set; }

        public int CategoryId { get; set; }

        // Hình đại diện
        public string? ImageUrl { get; set; }

        // Danh sách hình
>>>>>>> ee325eaf63f2aabb046ebc4c33770f92d4a56eca
        public List<string>? ImageUrls { get; set; }
    }
}