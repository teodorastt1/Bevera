using Bevera.Data;
using Bevera.Extensions;
using Bevera.Models.Catalog;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminCategoriesController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /AdminCategories
        public async Task<IActionResult> Index(string? q, DateTime? from, DateTime? to, int page = 1, int pageSize = 10)
        {
            IQueryable<Category> query = _db.Categories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(c => c.Name.Contains(q));
            }

            if (from.HasValue)
                query = query.Where(c => c.CreatedAt >= from.Value.Date);

            if (to.HasValue)
            {
                // inclusive до края на деня
                var end = to.Value.Date.AddDays(1);
                query = query.Where(c => c.CreatedAt < end);
            }

            query = query.OrderByDescending(c => c.CreatedAt);

            var paged = await query.ToPagedAsync(page, pageSize);

            ViewBag.Q = q;
            ViewBag.PageSize = pageSize;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");

            return View(paged);
        }

        // GET: /AdminCategories/Create
        public IActionResult Create()
        {
            return View(new AdminCategoryFormViewModel());
        }

        // POST: /AdminCategories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var category = new Category
            {
                Name = model.Name.Trim(),
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                category.ImagePath = await SaveCategoryImageAsync(model.ImageFile);
            }

            _db.Categories.Add(category);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminCategories/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            var vm = new AdminCategoryFormViewModel
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                ExistingImagePath = c.ImagePath
            };

            return View(vm);
        }

        // POST: /AdminCategories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminCategoryFormViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == model.Id);
            if (c == null) return NotFound();

            c.Name = model.Name.Trim();
            c.IsActive = model.IsActive;

            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                // delete old file
                DeletePhysicalFileIfExists(c.ImagePath);

                c.ImagePath = await SaveCategoryImageAsync(model.ImageFile);
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: /AdminCategories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Categories.FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();

            // optionally: block delete if has products
            // var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
            // if (hasProducts) { TempData["Error"] = "Category has products."; return RedirectToAction(nameof(Index)); }

            DeletePhysicalFileIfExists(c.ImagePath);

            _db.Categories.Remove(c);
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // Helpers (image upload)
        // =========================
        private async Task<string> SaveCategoryImageAsync(IFormFile file)
        {
            // basic size check
            if (file.Length > 5 * 1024 * 1024)
                throw new Exception("Файлът е твърде голям (макс 5 MB).");

            var allowedExt = new[] { ".png", ".jpg", ".jpeg", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExt.Contains(ext))
                throw new Exception("Непозволен тип файл! (png/jpg/webp)");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "categories");
            Directory.CreateDirectory(uploadsFolder);

            var stored = $"{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(uploadsFolder, stored);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            // return public path
            return $"/uploads/categories/{stored}";
        }

        private void DeletePhysicalFileIfExists(string? publicPath)
        {
            if (string.IsNullOrWhiteSpace(publicPath)) return;

            // publicPath example: /uploads/categories/xxx.jpg
            var relative = publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}
