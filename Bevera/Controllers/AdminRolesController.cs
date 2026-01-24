using Bevera.Models;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminRolesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminRolesController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /AdminRoles
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var model = new List<UserRoleRowViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);

                model.Add(new UserRoleRowViewModel
                {
                    UserId = u.Id,
                    Email = u.Email ?? "(no email)",

                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    PhoneNumber = u.PhoneNumber ?? "",
                    Address = u.Address ?? "",

                    RoleName = roles.FirstOrDefault()
                });

            }

            return View(model);
        }

        // GET: /AdminRoles/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (id == currentUserId)
            {
                return Forbid(); // или RedirectToAction(nameof(Index))
            }

            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            var selectedRole = currentRoles.FirstOrDefault();

            var allRoles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();

            var vm = new EditUserRoleViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                RoleName = selectedRole,
                Roles = allRoles.Select(r => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = r.Name,
                    Value = r.Name,
                    Selected = r.Name == selectedRole
                }).ToList()
            };

            return View(vm);
        }

        // POST: /AdminRoles/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserRoleViewModel vm)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (vm.UserId == currentUserId)
            {
                return Forbid(); // или RedirectToAction(nameof(Index))
            }

            if (string.IsNullOrWhiteSpace(vm.UserId))
                return NotFound();

            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null)
                return NotFound();

            // ако някой е махнал RoleName -> остава без роля
            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Any())
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrWhiteSpace(vm.RoleName))
                await _userManager.AddToRoleAsync(user, vm.RoleName);

            return RedirectToAction(nameof(Index));
        }
    }
}
