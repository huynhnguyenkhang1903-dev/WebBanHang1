using Websitebanhang.Models;

namespace Websitebanhang.Repositores
{
    public class MockCategoryRepository : ICategoryRepository
    {
        private List<Category> _categories;

        public MockCategoryRepository()
        {
            _categories = new List<Category>
            {
                new Category { Id = 1, Name = "Cà phê hạt" },
                new Category { Id = 2, Name = "Cà phê pha sẵn" },
                new Category { Id = 3, Name = "Quà tặng & Bộ quà tặng" },
                new Category { Id = 4, Name = "Nguyên liệu pha chế" },
                new Category { Id = 5, Name = "Dụng cụ pha chế" }
            };
        }

        public IEnumerable<Category> GetAll()
        {
            return _categories;
        }

        public Category? GetById(int id)
        {
            return _categories.FirstOrDefault(c => c.Id == id);
        }

        public void Add(Category category)
        {
            category.Id = _categories.Max(c => c.Id) + 1;
            _categories.Add(category);
        }

        public void Update(Category category)
        {
            var index = _categories.FindIndex(c => c.Id == category.Id);
            if (index != -1)
            {
                _categories[index] = category;
            }
        }

        public void Delete(int id)
        {
            var category = _categories.FirstOrDefault(c => c.Id == id);
            if (category != null)
            {
                _categories.Remove(category);
            }
        }
    }
}