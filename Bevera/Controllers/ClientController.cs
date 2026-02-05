using Bevera.Data;
using Bevera.Models;
using Bevera.Models.Catalog;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bevera.Controllers
{
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClientController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // =========================
        // CATEGORIES
        // =========================
        [HttpGet]
        public async Task<IActionResult> Category(int id)
        {
            // ако Category не е активна -> 404 (нормално)
            var category = await _db.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);

            if (category == null)
                return NotFound();

            var products = await _db.Products
                .AsNoTracking()
                .Where(p => p.CategoryId == id && p.IsActive)
                .Include(p => p.Images)
                .OrderBy(p => p.Name)
                .ToListAsync();

            ViewBag.CategoryName = category.Name;

            // трябва да имаш Views/Client/Category.cshtml
            return View(products);
        }

        // =========================
        // PRODUCT DETAILS
        // =========================
        [HttpGet]
        public async Task<IActionResult> Product(int id)
        {
            var p = await _db.Products
                .AsNoTracking()
                .Include(x => x.Images)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (p == null)
                return NotFound();

            // трябва да имаш Views/Client/Product.cshtml
            return View(p);
        }

        // =========================
        // PROFILE
        // =========================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var vm = new ClientProfileViewModel
            {
                FirstName = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                CreatedAt = user.CreatedAt,
                Orders = new List<OrderRowVm>()
            };

            // IMPORTANT:
            // В твоя Order модел НЯМА CreatedAt.
            // Затова тук ползваме ChangedAt като "дата на създаване/последна промяна".
            // Ако после добавиш CreatedAt, просто сменяш ChangedAt -> CreatedAt.
            vm.Orders = await _db.Orders
                .AsNoTracking()
                .Where(o => o.ClientId == user.Id)
                .OrderByDescending(o => o.ChangedAt)
                .Select(o => new OrderRowVm
                {
                    OrderId = o.Id,
                    CreatedAt = o.ChangedAt,
                    Total = o.Total,
                    Status = o.Status
                })
                .ToListAsync();

            return View(vm); // Views/Client/Profile.cshtml
        }

        // =========================
        // FAVORITES LIST
        // =========================
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var favorites = await _db.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                    .ThenInclude(p => p.Images)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            // ТУК подаваш List<Favorite> -> View-то трябва да е @model List<Favorite> или IEnumerable<Favorite>
            return View(favorites); // Views/Client/Favorites.cshtml
        }

        // =========================
        // TOGGLE FAVORITE
        // =========================
        [Authorize]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int productId, string? returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var existing = await _db.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (existing == null)
            {
                _db.Favorites.Add(new Favorite
                {
                    UserId = userId,
                    ProductId = productId
                });
            }
            else
            {
                _db.Favorites.Remove(existing);
            }

            await _db.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Favorites));
        }
    }
}
