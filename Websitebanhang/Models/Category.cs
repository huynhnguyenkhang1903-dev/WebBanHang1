<<<<<<< HEAD
﻿namespace Websitebanhang.Models
=======
﻿using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models
>>>>>>> ee325eaf63f2aabb046ebc4c33770f92d4a56eca
{
    public class Category
    {
        public int Id { get; set; }
<<<<<<< HEAD

        public string Name { get; set; } = "";
    }
}
=======
        [Required, StringLength(50)]
        public string? Name { get; set; }
    }
}
>>>>>>> ee325eaf63f2aabb046ebc4c33770f92d4a56eca
