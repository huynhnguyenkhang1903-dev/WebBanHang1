<<<<<<< HEAD
﻿using Websitebanhang.Models;

public interface IProductRepository
{
    IEnumerable<Product> GetAll();

    Product? GetById(int id);

    void Add(Product product);

    void Update(Product product);

    void Delete(int id);
}
=======
﻿using System.Collections.Generic;
using Websitebanhang.Models;
public interface IProductRepository
{
    IEnumerable<Product> GetAll();
    Product? GetById(int id);
    void Add(Product product);
    void Update(Product product);
    void Delete(int id);
}
>>>>>>> ee325eaf63f2aabb046ebc4c33770f92d4a56eca
