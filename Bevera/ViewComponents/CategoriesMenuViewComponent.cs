using Bevera.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.ViewComponents
{
    public class CategoriesMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _db;

        public CategoriesMenuViewComponent(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cats = await _db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryMenuItem
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            return View(cats);
        }

        public class CategoryMenuItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }
    }
}
