using Bevera.Data;
using Bevera.Models;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Client,Admin,Worker")]
    public class ClientController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ClientController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var orders = await _db.Orders
                .Where(o => o.ClientId == user.Id)
                .OrderByDescending(o => o.ChangedAt)
                .Select(o => new ClientOrderRowViewModel
                {
                    OrderId = o.Id,
                    Status = o.Status,
                    Total = o.Total,
                    CreatedAt = o.ChangedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm")
                })
                .ToListAsync();

            var vm = new ClientProfileViewModel
            {
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Orders = orders
            };

            return View(vm);
        }

        [Authorize(Roles = "Client")]
        public async Task<IActionResult> Favorites()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _db.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                .AsNoTracking()
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();

            return View(items);
        }
    }
}
