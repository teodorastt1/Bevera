using Bevera.Data;
using Bevera.Helpers;
using Bevera.Models;
using Bevera.Models.ViewModels.Distributor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Distributor,Admin")]
    public class DistributorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public DistributorController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Products(int? distributorId = null)
        {
            return RedirectToAction(nameof(Orders), new { distributorId });
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? distributorId = null)
        {
            var distributor = await ResolveDistributorAsync(distributorId);
            if (distributor == null)
                return NotFound();

            var ordersQuery = _db.PurchaseOrders
                .AsNoTracking()
                .Where(x => x.DistributorId == distributor.Id);

            var vm = new DistributorDashboardVm
            {
                DistributorId = distributor.Id,
                DistributorName = distributor.Name,
                IsPreviewMode = User.IsInRole("Admin"),
                NewOrdersCount = await ordersQuery.CountAsync(x => x.Status == PurchaseOrderStates.SentToDistributor),
                PreparingOrdersCount = await ordersQuery.CountAsync(x => x.Status == PurchaseOrderStates.InPreparation),
                CompletedOrdersCount = await ordersQuery.CountAsync(x => x.Status == PurchaseOrderStates.SentToAdmin || x.Status == PurchaseOrderStates.Paid),
                TotalRevenue = await ordersQuery
                    .Where(x => x.Status == PurchaseOrderStates.SentToAdmin || x.Status == PurchaseOrderStates.Paid)
                    .SumAsync(x => (decimal?)x.TotalAmount) ?? 0m
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(DistributorProductManageVm vm, int? distributorId = null)
        {
            var distributor = await ResolveDistributorAsync(distributorId);
            if (distributor == null)
                return NotFound();

            var entity = await _db.DistributorProducts
                .FirstOrDefaultAsync(x => x.Id == vm.Id && x.DistributorId == distributor.Id);

            if (entity == null)
                return NotFound();

            entity.IsAvailable = vm.IsAvailable;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["ToastMessage"] = "Каталогът е обновен успешно.";
            TempData["ToastType"] = "success";
            TempData["ToastScope"] = "distributor";

            return RedirectToAction(nameof(Orders), new { distributorId = distributor.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Orders(string? tab = null, int? distributorId = null)
        {
            var distributor = await ResolveDistributorAsync(distributorId);
            if (distributor == null)
                return NotFound();

            var vm = new DistributorOrdersVm
            {
                DistributorId = distributor.Id,
                DistributorName = distributor.Name,
                IsPreviewMode = User.IsInRole("Admin"),
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "preparing" : tab,
                Orders = await _db.PurchaseOrders
                    .Include(x => x.Items)
                    .Where(x => x.DistributorId == distributor.Id)
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new DistributorOrderListRowVm
                    {
                        Id = x.Id,
                        Status = x.Status,
                        TotalAmount = x.TotalAmount,
                        CreatedAt = x.CreatedAt,
                        SubmittedAt = x.SubmittedAt,
                        ReceivedAt = x.ReceivedAt,
                        ItemsCount = x.Items.Count
                    })
                    .ToListAsync()
            };

            return View(vm);
        }
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id, int? distributorId = null)
        {
            var distributor = await ResolveDistributorAsync(distributorId);
            if (distributor == null)
                return NotFound();

            var order = await _db.PurchaseOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && x.DistributorId == distributor.Id);

            if (order == null)
                return NotFound();

            var vm = new DistributorOrderDetailsVm
            {
                DistributorId = distributor.Id,
                DistributorName = distributor.Name,
                OrderId = order.Id,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt,
                SubmittedAt = order.SubmittedAt,
                ReceivedAt = order.ReceivedAt,
                Notes = order.Notes,
                IsPreviewMode = User.IsInRole("Admin"),
                Items = order.Items
                    .OrderBy(x => x.ProductName)
                    .Select(x => new DistributorOrderDetailsItemVm
                    {
                        Id = x.Id,
                        ProductName = x.ProductName,
                        Quantity = x.Quantity,
                        CostPrice = x.CostPrice,
                        LineTotal = x.LineTotal,
                        UnitsPerCase = 12
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartPreparation(int id, int? distributorId = null)
        {
            var distributor = await ResolveDistributorAsync(distributorId);
            if (distributor == null)
                return NotFound();

            var order = await _db.PurchaseOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && x.DistributorId == distributor.Id);

            if (order == null)
                return NotFound();

            if (order.Status != PurchaseOrderStates.SentToDistributor)
            {
                TempData["ToastMessage"] = "Само нова заявка може да бъде взета за подготовка.";
                TempData["ToastType"] = "warning";
                TempData["ToastScope"] = "distributor";
                return RedirectToAction(nameof(OrderDetails), new { id, distributorId = distributor.Id });
            }

            order.Status = PurchaseOrderStates.InPreparation;
            await _db.SaveChangesAsync();

            TempData["ToastMessage"] = "Подготовката започна успешно.";
            TempData["ToastType"] = "success";
            TempData["ToastScope"] = "distributor";

            return RedirectToAction(nameof(OrderDetails), new { id, distributorId = distributor.Id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendOrderToAdmin(int id, List<DistributorOrderPriceInputVm> items, int? distributorId = null)
        {
            var distributor = await ResolveDistributorAsync(distributorId);
            if (distributor == null)
                return NotFound();

            var order = await _db.PurchaseOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && x.DistributorId == distributor.Id);

            if (order == null)
                return NotFound();

            if (order.Status != PurchaseOrderStates.InPreparation)
            {
                TempData["ToastMessage"] = "Само заявка в подготовка може да бъде изпратена.";
                TempData["ToastType"] = "warning";
                TempData["ToastScope"] = "distributor";
                return RedirectToAction(nameof(OrderDetails), new { id, distributorId = distributor.Id });
            }

            if (!order.Items.Any())
            {
                TempData["ToastMessage"] = "Заявката няма продукти.";
                TempData["ToastType"] = "error";
                TempData["ToastScope"] = "distributor";
                return RedirectToAction(nameof(OrderDetails), new { id, distributorId = distributor.Id });
            }

            if (items == null || !items.Any())
            {
                TempData["ToastMessage"] = "Липсват въведени цени за продуктите.";
                TempData["ToastType"] = "error";
                TempData["ToastScope"] = "distributor";
                return RedirectToAction(nameof(OrderDetails), new { id, distributorId = distributor.Id });
            }

            decimal totalAmount = 0m;

            foreach (var dbItem in order.Items)
            {
                var postedItem = items.FirstOrDefault(x => x.Id == dbItem.Id);
                if (postedItem == null)
                {
                    TempData["ToastMessage"] = $"Липсва цена за продукт: {dbItem.ProductName}.";
                    TempData["ToastType"] = "error";
                    TempData["ToastScope"] = "distributor";
                    return RedirectToAction(nameof(OrderDetails), new { id, distributorId = distributor.Id });
                }

                var normalizedPrice = (postedItem.UnitPrice ?? string.Empty)
                    .Trim()
                    .Replace(" ", "")
                    .Replace(",", ".");

                if (!decimal.TryParse(
                        normalizedPrice,
                        System.Globalization.NumberStyles.AllowDecimalPoint,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var parsedUnitPrice))
                {
                    TempData["ToastMessage"] = $"Въведи валидна цена за 1 брой за продукт: {dbItem.ProductName}.";
                    TempData["ToastType"] = "error";
                    TempData["ToastScope"] = "distributor";
                    return RedirectToAction(nameof(OrderDetails), new { id, distributorId = distributor.Id });
                }

                if (parsedUnitPrice <= 0)
                {
                    TempData["ToastMessage"] = $"Въведи валидна цена за 1 брой за продукт: {dbItem.ProductName}.";
                    TempData["ToastType"] = "error";
                    TempData["ToastScope"] = "distributor";
                    return RedirectToAction(nameof(OrderDetails), new { id, distributorId = distributor.Id });
                }

                var lineTotal = Math.Round(parsedUnitPrice * dbItem.Quantity, 2, MidpointRounding.AwayFromZero);

                dbItem.CostPrice = Math.Round(parsedUnitPrice, 2, MidpointRounding.AwayFromZero);
                dbItem.LineTotal = lineTotal;

                totalAmount += lineTotal;
            }

            order.TotalAmount = totalAmount;
            order.Status = PurchaseOrderStates.SentToAdmin;
            order.SubmittedAt = DateTime.UtcNow;

            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in adminUsers.Take(5))
            {
                _db.AppNotifications.Add(new AppNotification
                {
                    UserId = admin.Id,
                    Message = $"Дистрибуторът изпрати заявка #{order.Id} към админ.",
                    Type = "Supply",
                    Url = $"/AdminSupply/PurchaseOrderDetails/{order.Id}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            TempData["ToastMessage"] = "Заявката е изпратена успешно към админ.";
            TempData["ToastType"] = "success";
            TempData["ToastScope"] = "distributor";

            return RedirectToAction(nameof(OrderDetails), new { id, distributorId = distributor.Id });
        }

        private async Task<Bevera.Models.Supply.Distributor?> ResolveDistributorAsync(int? distributorId)
        {
            if (User.IsInRole("Admin"))
            {
                if (distributorId.HasValue)
                    return await _db.Distributors.FirstOrDefaultAsync(x => x.Id == distributorId.Value);

                return await _db.Distributors.OrderBy(x => x.Name).FirstOrDefaultAsync();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return null;

            var distributor = await _db.Distributors.FirstOrDefaultAsync(x =>
                x.IsActive &&
                ((x.ApplicationUserId != null && x.ApplicationUserId == user.Id) ||
                 (x.Email != null && x.Email == user.Email)));

            if (distributor != null)
                return distributor;

            return await _db.Distributors.Where(x => x.IsActive).OrderBy(x => x.Name).FirstOrDefaultAsync();
        }
    }
}
