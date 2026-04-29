using Bevera.Data;
using Bevera.Helpers;
using Bevera.Models;
using Bevera.Models.Catalog;
using Bevera.Models.Finance;
using Bevera.Models.Inventory;
using Bevera.Models.Supply;
using Bevera.Models.ViewModels.Supply;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSupplyController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminSupplyController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // =========================
        // DASHBOARD
        // =========================
        public async Task<IActionResult> Index()
        {
            var balance = await EnsureCompanyBalanceAsync();

            var totalIncome = await _db.FinanceTransactions
                .Where(x => x.Type == FinanceTypes.Income)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var totalExpenses = await _db.FinanceTransactions
                .Where(x => x.Type == FinanceTypes.Expense)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var grossProfit = await _db.OrderItems
                .Include(x => x.Product)
                .SumAsync(x => (decimal?)((x.UnitPrice - (x.Product != null ? x.Product.CostPrice : 0m)) * x.Quantity)) ?? 0m;

            var vm = new SupplyDashboardVm
            {
                CompanyBalance = balance.Balance,
                TotalIncome = totalIncome,
                TotalExpenses = totalExpenses,
                GrossProfit = grossProfit,
                DistributorsCount = await _db.Distributors.CountAsync(),

                DraftOrdersCount = await _db.PurchaseOrders.CountAsync(x =>
                    x.Status == PurchaseOrderStates.Draft),

                SubmittedOrdersCount = await _db.PurchaseOrders.CountAsync(x =>
                    x.Status == PurchaseOrderStates.SentToDistributor ||
                    x.Status == PurchaseOrderStates.InPreparation ||
                    x.Status == PurchaseOrderStates.SentToAdmin),

                ReceivedOrdersCount = await _db.PurchaseOrders.CountAsync(x =>
                    x.Status == PurchaseOrderStates.Paid),

                LatestOrders = await _db.PurchaseOrders
                    .Include(x => x.Distributor)
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(10)
                    .Select(x => new PurchaseOrderShortVm
                    {
                        Id = x.Id,
                        DistributorName = x.Distributor.Name,
                        Status = x.Status,
                        TotalAmount = x.TotalAmount,
                        CreatedAt = x.CreatedAt
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        // =========================
        // DISTRIBUTORS
        // =========================
        public async Task<IActionResult> Distributors()
        {
            var distributors = await _db.Distributors
                .OrderBy(x => x.Name)
                .ToListAsync();

            return View(distributors);
        }

        [HttpGet]
        public IActionResult CreateDistributor()
        {
            TempData["Error"] = "Админът няма право да създава дистрибутори ръчно. Дистрибуторът трябва сам да се регистрира, а после му смени ролята.";
            TempData["ToastScope"] = "admin";
            return RedirectToAction(nameof(Distributors));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDistributor(DistributorFormVm vm)
        {
            TempData["Error"] = "Админът няма право да създава дистрибутори ръчно. Използвай регистрация + смяна на роля.";
            TempData["ToastScope"] = "admin";
            return RedirectToAction(nameof(Distributors));
        }

        [HttpGet]
        public async Task<IActionResult> EditDistributor(int id)
        {
            var entity = await _db.Distributors.FindAsync(id);
            if (entity == null) return NotFound();

            var vm = new DistributorFormVm
            {
                Id = entity.Id,
                Name = entity.Name,
                Email = entity.Email,
                Phone = entity.Phone,
                Address = entity.Address,
                Notes = entity.Notes,
                IsActive = entity.IsActive
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDistributor(DistributorFormVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var entity = await _db.Distributors.FindAsync(vm.Id);
            if (entity == null) return NotFound();

            entity.Name = vm.Name;
            entity.Email = vm.Email;
            entity.Phone = vm.Phone;
            entity.Address = vm.Address;
            entity.Notes = vm.Notes;
            entity.IsActive = vm.IsActive;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Дистрибуторът е редактиран успешно.";
            TempData["ToastScope"] = "admin";
            return RedirectToAction(nameof(Distributors));
        }

        // =========================
        // DISTRIBUTOR PRODUCTS
        // =========================
        [HttpGet]
        public async Task<IActionResult> DistributorProducts(int distributorId)
        {
            var distributor = await _db.Distributors.FindAsync(distributorId);
            if (distributor == null) return NotFound();

            ViewBag.Distributor = distributor;

            var items = await _db.DistributorProducts
                .Include(x => x.Product)
                .Where(x => x.DistributorId == distributorId)
                .OrderBy(x => x.Product.Name)
                .Select(x => new DistributorProductVm
                {
                    Id = x.Id,
                    DistributorId = x.DistributorId,
                    ProductId = x.ProductId,
                    ProductName = x.Product.Name,
                    CostPrice = x.CostPrice,
                    IsAvailable = x.IsAvailable
                })
                .ToListAsync();

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> AddDistributorProduct(int distributorId)
        {
            var distributor = await _db.Distributors.FindAsync(distributorId);
            if (distributor == null) return NotFound();

            var usedProductIds = await _db.DistributorProducts
                .Where(x => x.DistributorId == distributorId)
                .Select(x => x.ProductId)
                .ToListAsync();

            var vm = new PurchaseOrderItemCreateVm
            {
                Products = await _db.Products
                    .Where(x => x.IsActive && !usedProductIds.Contains(x.Id))
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToListAsync()
            };

            ViewBag.Distributor = distributor;
            ViewBag.DistributorId = distributorId;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDistributorProduct(int distributorId, PurchaseOrderItemCreateVm vm)
        {
            var distributor = await _db.Distributors.FindAsync(distributorId);
            if (distributor == null) return NotFound();

            if (await _db.DistributorProducts.AnyAsync(x => x.DistributorId == distributorId && x.ProductId == vm.ProductId))
            {
                ModelState.AddModelError("", "Този продукт вече е добавен към дистрибутора.");
            }

            if (!ModelState.IsValid)
            {
                vm.Products = await _db.Products
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToListAsync();

                ViewBag.Distributor = distributor;
                ViewBag.DistributorId = distributorId;
                return View(vm);
            }

            var entity = new DistributorProduct
            {
                DistributorId = distributorId,
                ProductId = vm.ProductId,
                CostPrice = vm.CostPrice,
                IsAvailable = true,
                UpdatedAt = DateTime.UtcNow
            };

            _db.DistributorProducts.Add(entity);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Продуктът е добавен към каталога на дистрибутора.";
            TempData["ToastScope"] = "admin";
            return RedirectToAction(nameof(DistributorProducts), new { distributorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDistributorProduct(DistributorProductVm vm)
        {
            var entity = await _db.DistributorProducts.FindAsync(vm.Id);
            if (entity == null) return NotFound();

            entity.CostPrice = vm.CostPrice;
            entity.IsAvailable = vm.IsAvailable;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["Success"] = "Продуктът е обновен успешно.";
            TempData["ToastScope"] = "admin";
            return RedirectToAction(nameof(DistributorProducts), new { distributorId = entity.DistributorId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDistributorProduct(int id)
        {
            var entity = await _db.DistributorProducts.FindAsync(id);
            if (entity == null) return NotFound();

            var distributorId = entity.DistributorId;

            _db.DistributorProducts.Remove(entity);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Продуктът е премахнат от каталога на дистрибутора.";
            TempData["ToastScope"] = "admin";
            return RedirectToAction(nameof(DistributorProducts), new { distributorId });
        }

        // =========================
        // PURCHASE ORDERS
        // =========================
        [HttpGet]
        public async Task<IActionResult> PurchaseOrders()
        {
            var orders = await _db.PurchaseOrders
                .Include(x => x.Distributor)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        [HttpGet]
        public async Task<IActionResult> CreatePurchaseOrder()
        {
            var vm = new PurchaseOrderCreateVm
            {
                Distributors = await _db.Distributors
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePurchaseOrder(PurchaseOrderCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Distributors = await _db.Distributors
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Name)
                    .Select(x => new SelectListItem
                    {
                        Value = x.Id.ToString(),
                        Text = x.Name
                    })
                    .ToListAsync();

                return View(vm);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var entity = new PurchaseOrder
            {
                DistributorId = vm.DistributorId,
                Status = PurchaseOrderStates.Draft,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = user.Id,
                Notes = vm.Notes,
                TotalAmount = 0m
            };

            _db.PurchaseOrders.Add(entity);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Черновата е създадена успешно.";
            TempData["ToastScope"] = "admin";
            return RedirectToAction(nameof(PurchaseOrderDetails), new { id = entity.Id });
        }

        [HttpGet]
        public async Task<IActionResult> PurchaseOrderDetails(int id)
        {
            var entity = await _db.PurchaseOrders
                .Include(x => x.Distributor)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) return NotFound();

            var vm = new PurchaseOrderDetailsVm
            {
                Id = entity.Id,
                DistributorName = entity.Distributor.Name,
                Status = entity.Status,
                CreatedAt = entity.CreatedAt,
                SubmittedAt = entity.SubmittedAt,
                ReceivedAt = entity.ReceivedAt,
                TotalAmount = entity.TotalAmount,
                Notes = entity.Notes,
                Items = entity.Items
                    .OrderBy(x => x.ProductName)
                    .Select(x => new PurchaseOrderItemRowVm
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        Quantity = x.Quantity,
                        CostPrice = x.CostPrice,
                        LineTotal = x.LineTotal,
                        UnitsPerCase = 12
                    })
                    .ToList()
            };

            ViewBag.CanEdit = entity.Status == PurchaseOrderStates.Draft;
            ViewBag.CanSubmit = entity.Status == PurchaseOrderStates.Draft && entity.Items.Any();
            ViewBag.CanPayAndLoad = entity.Status == PurchaseOrderStates.SentToAdmin;

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> AddPurchaseOrderItem(int purchaseOrderId)
        {
            var po = await _db.PurchaseOrders
                .Include(x => x.Distributor)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == purchaseOrderId);

            if (po == null) return NotFound();

            if (po.Status != PurchaseOrderStates.Draft)
            {
                TempData["Error"] = "Можеш да добавяш продукти само към чернова.";
                TempData["ToastScope"] = "admin";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id = purchaseOrderId });
            }

            var distributorProducts = await _db.DistributorProducts
                .Include(x => x.Product)
                .Where(x => x.DistributorId == po.DistributorId)
                .ToListAsync();

            var priceMap = distributorProducts.ToDictionary(x => x.ProductId, x => x.CostPrice);
            var availableMap = distributorProducts.ToDictionary(x => x.ProductId, x => x.IsAvailable);
            var existingProductIds = po.Items.Select(x => x.ProductId).ToHashSet();

            var allProducts = await _db.Products
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var vm = new PurchaseOrderItemCreateVm
            {
                PurchaseOrderId = purchaseOrderId,
                Items = allProducts.Select(x => new PurchaseOrderItemBulkRowVm
                {
                    ProductId = x.Id,
                    ProductName = x.Name + (existingProductIds.Contains(x.Id) ? " (вече е добавен)" : ""),
                    CasesCount = 1,
                    UnitsPerCase = 12,
                    CostPrice = priceMap.TryGetValue(x.Id, out var price) ? price : 0m,
                    HasDistributorPrice = priceMap.ContainsKey(x.Id),
                    IsAvailable = availableMap.TryGetValue(x.Id, out var available) ? available : true
                }).ToList()
            };

            ViewBag.DistributorName = po.Distributor.Name;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(ValueCountLimit = 20000)]
        public async Task<IActionResult> AddPurchaseOrderItem(PurchaseOrderItemCreateVm vm)
        {
            if (vm.Items == null)
                vm.Items = new List<PurchaseOrderItemBulkRowVm>();

            var po = await _db.PurchaseOrders
                .Include(x => x.Items)
                .Include(x => x.Distributor)
                .FirstOrDefaultAsync(x => x.Id == vm.PurchaseOrderId);

            if (po == null) return NotFound();

            if (po.Status != PurchaseOrderStates.Draft)
            {
                TempData["Error"] = "Можеш да добавяш продукти само към чернова.";
                TempData["ToastScope"] = "admin";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id = vm.PurchaseOrderId });
            }

            var selectedItems = vm.Items
                .Where(x => x.IsSelected)
                .ToList();

            if (!selectedItems.Any())
            {
                ModelState.AddModelError("", "Избери поне един продукт.");
            }

            foreach (var row in selectedItems)
            {
                if (row.CasesCount < 1)
                    ModelState.AddModelError("", $"Невалиден брой кашони за {row.ProductName}.");

                if (row.UnitsPerCase < 1 || row.UnitsPerCase > 120)
                    ModelState.AddModelError("", $"Невалиден брой в кашон за {row.ProductName}.");
            }

            if (!ModelState.IsValid)
            {
                await PopulatePurchaseOrderItemVmAsync(vm, po.DistributorId, po.Distributor.Name);
                return View(vm);
            }

            foreach (var row in selectedItems)
            {
                var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == row.ProductId && x.IsActive);
                if (product == null)
                    continue;

                var distributorProduct = await _db.DistributorProducts
                    .FirstOrDefaultAsync(x => x.DistributorId == po.DistributorId && x.ProductId == row.ProductId);

                if (distributorProduct == null)
                {
                    distributorProduct = new DistributorProduct
                    {
                        DistributorId = po.DistributorId,
                        ProductId = row.ProductId,
                        CostPrice = 0m,
                        IsAvailable = true,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _db.DistributorProducts.Add(distributorProduct);
                }

                var appliedCostPrice = distributorProduct.CostPrice;
                var requestedUnits = row.RequestedUnits;

                if (requestedUnits <= 0)
                    continue;

                var existingItem = po.Items.FirstOrDefault(x => x.ProductId == row.ProductId);

                if (existingItem != null)
                {
                    existingItem.Quantity += requestedUnits;
                    existingItem.CostPrice = appliedCostPrice;
                    existingItem.LineTotal = existingItem.Quantity * appliedCostPrice;
                }
                else
                {
                    _db.PurchaseOrderItems.Add(new PurchaseOrderItem
                    {
                        PurchaseOrderId = po.Id,
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = requestedUnits,
                        CostPrice = appliedCostPrice,
                        LineTotal = requestedUnits * appliedCostPrice
                    });
                }
            }

            await _db.SaveChangesAsync();
            await RecalculatePurchaseOrderTotal(po.Id);

            TempData["Success"] = "Продуктите са добавени в черновата.";
            TempData["ToastScope"] = "admin";

            return RedirectToAction(nameof(PurchaseOrderDetails), new { id = po.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePurchaseOrderItem(int id)
        {
            var item = await _db.PurchaseOrderItems.FindAsync(id);
            if (item == null) return NotFound();

            var po = await _db.PurchaseOrders.FindAsync(item.PurchaseOrderId);
            if (po == null) return NotFound();

            if (po.Status != PurchaseOrderStates.Draft)
            {
                TempData["Error"] = "Можеш да триеш продукти само от чернова.";
                TempData["ToastScope"] = "admin";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id = po.Id });
            }

            _db.PurchaseOrderItems.Remove(item);
            await _db.SaveChangesAsync();

            await RecalculatePurchaseOrderTotal(po.Id);

            TempData["Success"] = "Продуктът е премахнат от черновата.";
            TempData["ToastScope"] = "admin";
            return RedirectToAction(nameof(PurchaseOrderDetails), new { id = po.Id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPurchaseOrder(int id)
        {
            var po = await _db.PurchaseOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (po == null) return NotFound();

            if (po.Status != PurchaseOrderStates.Draft)
            {
                TempData["Error"] = "Само чернова може да бъде изпратена.";
                TempData["ToastScope"] = "admin";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
            }

            if (!po.Items.Any())
            {
                TempData["Error"] = "Не можеш да изпратиш празна заявка.";
                TempData["ToastScope"] = "admin";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
            }

            po.Status = PurchaseOrderStates.SentToDistributor;
            po.SubmittedAt = DateTime.UtcNow;

            var distributorUserId = await FindDistributorUserIdAsync(po.DistributorId);
            if (!string.IsNullOrWhiteSpace(distributorUserId))
            {
                _db.AppNotifications.Add(new AppNotification
                {
                    UserId = distributorUserId,
                    Message = $"Имаш нова заявка #{po.Id} за подготовка.",
                    Type = "Supply",
                    Url = $"/Distributor/OrderDetails/{po.Id}?distributorId={po.DistributorId}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Заявката е изпратена към дистрибутора.";
            TempData["ToastScope"] = "admin";
            return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAndLoadPurchaseOrder(int id)
        {
            var po = await _db.PurchaseOrders
                .Include(x => x.Items)
                .Include(x => x.Distributor)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (po == null) return NotFound();

            if (po.Status != PurchaseOrderStates.SentToAdmin)
            {
                TempData["Error"] = "Само заявка, върната от дистрибутора, може да се плати и зареди.";
                TempData["ToastScope"] = "admin";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
            }

            var balance = await EnsureCompanyBalanceAsync();

            if (balance.Balance < po.TotalAmount)
            {
                TempData["Error"] = $"Недостатъчен фирмен баланс. Налични: {balance.Balance:0.00} €, нужни: {po.TotalAmount:0.00} €.";
                TempData["ToastScope"] = "admin";
                return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in po.Items)
                {
                    var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == item.ProductId);
                    if (product == null)
                        continue;

                    product.StockQty += item.Quantity;
                    product.CostPrice = item.CostPrice;

                    _db.InventoryMovements.Add(new InventoryMovement
                    {
                        ProductId = product.Id,
                        QuantityDelta = item.Quantity,
                        Type = "IN",
                        Note = $"Заредено по заявка #{po.Id}",
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = user.Id
                    });
                }

                balance.Balance -= po.TotalAmount;
                balance.UpdatedAt = DateTime.UtcNow;

                _db.FinanceTransactions.Add(new FinanceTransaction
                {
                    Type = FinanceTypes.Expense,
                    Source = FinanceSources.PurchaseOrder,
                    Amount = po.TotalAmount,
                    Description = $"Плащане към дистрибутор {po.Distributor.Name} по заявка #{po.Id}",
                    CreatedAt = DateTime.UtcNow,
                    PurchaseOrderId = po.Id,
                    CreatedByUserId = user.Id
                });

                po.Status = PurchaseOrderStates.Paid;
                po.ReceivedAt = DateTime.UtcNow;
                po.ReceivedByUserId = user.Id;

                var distributorUserId = await FindDistributorUserIdAsync(po.DistributorId);
                if (!string.IsNullOrWhiteSpace(distributorUserId))
                {
                    _db.AppNotifications.Add(new AppNotification
                    {
                        UserId = distributorUserId,
                        Message = $"Заявка #{po.Id} е платена и заредена успешно.",
                        Type = "Supply",
                        Url = $"/Distributor/OrderDetails/{po.Id}?distributorId={po.DistributorId}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Плащането мина успешно и артикулите са заредени.";
                TempData["ToastScope"] = "admin";
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Възникна проблем при плащането и зареждането.";
                TempData["ToastScope"] = "admin";
            }

            return RedirectToAction(nameof(PurchaseOrderDetails), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> PurchaseOrderInvoice(int id)
        {
            var entity = await _db.PurchaseOrders
                .Include(x => x.Distributor)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null) return NotFound();

            var vm = new PurchaseOrderDetailsVm
            {
                Id = entity.Id,
                DistributorName = entity.Distributor.Name,
                Status = entity.Status,
                TotalAmount = entity.TotalAmount,
                Notes = entity.Notes,
                CreatedAt = entity.CreatedAt,
                SubmittedAt = entity.SubmittedAt,
                ReceivedAt = entity.ReceivedAt,
                Items = entity.Items
                    .OrderBy(x => x.ProductName)
                    .Select(x => new PurchaseOrderItemRowVm
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
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

        // =========================
        // FINANCE
        // =========================
        public async Task<IActionResult> Finance()
        {
            var balance = await EnsureCompanyBalanceAsync();
            ViewBag.Balance = balance.Balance;

            var transactions = await _db.FinanceTransactions
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync();

            return View(transactions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SeedBalance(decimal amount)
        {
            if (amount < 0)
            {
                TempData["Error"] = "Сумата не може да е отрицателна.";
                TempData["ToastScope"] = "admin";
                return RedirectToAction(nameof(Finance));
            }

            var balance = await EnsureCompanyBalanceAsync();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var previousBalance = balance.Balance;
            var difference = amount - previousBalance;

            balance.Balance = amount;
            balance.UpdatedAt = DateTime.UtcNow;

            if (difference != 0)
            {
                _db.FinanceTransactions.Add(new FinanceTransaction
                {
                    Type = difference >= 0 ? FinanceTypes.Income : FinanceTypes.Expense,
                    Source = FinanceSources.Manual,
                    Amount = Math.Abs(difference),
                    Description = $"Ръчна корекция на фирмен баланс: {previousBalance:0.00} € -> {amount:0.00} €",
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = user.Id
                });
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Фирменият баланс е зададен успешно.";
            TempData["ToastScope"] = "admin";
            return RedirectToAction(nameof(Finance));
        }

        // =========================
        // HELPERS
        // =========================
        private async Task PopulatePurchaseOrderItemVmAsync(PurchaseOrderItemCreateVm vm, int distributorId, string distributorName)
        {
            var distributorProducts = await _db.DistributorProducts
                .Include(x => x.Product)
                .Where(x => x.DistributorId == distributorId)
                .ToListAsync();

            var priceMap = distributorProducts.ToDictionary(x => x.ProductId, x => x.CostPrice);
            var availableMap = distributorProducts.ToDictionary(x => x.ProductId, x => x.IsAvailable);

            var allProducts = await _db.Products
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            var postedMap = vm.Items?.ToDictionary(x => x.ProductId, x => x) ?? new Dictionary<int, PurchaseOrderItemBulkRowVm>();

            vm.Items = allProducts.Select(x =>
            {
                postedMap.TryGetValue(x.Id, out var existing);

                return new PurchaseOrderItemBulkRowVm
                {
                    ProductId = x.Id,
                    ProductName = x.Name,
                    IsSelected = existing?.IsSelected ?? false,
                    CasesCount = existing?.CasesCount ?? 1,
                    UnitsPerCase = existing?.UnitsPerCase ?? 12,
                    CostPrice = priceMap.TryGetValue(x.Id, out var price) ? price : 0m,
                    HasDistributorPrice = priceMap.ContainsKey(x.Id),
                    IsAvailable = availableMap.TryGetValue(x.Id, out var available) ? available : true
                };
            }).ToList();

            ViewBag.DistributorName = distributorName;
        }

        private async Task RecalculatePurchaseOrderTotal(int purchaseOrderId)
        {
            var po = await _db.PurchaseOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == purchaseOrderId);

            if (po == null) return;

            po.TotalAmount = po.Items.Sum(x => x.LineTotal);
            await _db.SaveChangesAsync();
        }

        private async Task<CompanyBalance> EnsureCompanyBalanceAsync()
        {
            var balance = await _db.CompanyBalances.FirstOrDefaultAsync();

            if (balance == null)
            {
                balance = new CompanyBalance
                {
                    Balance = 0m,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.CompanyBalances.Add(balance);
                await _db.SaveChangesAsync();
            }

            return balance;
        }

        private async Task<string?> FindDistributorUserIdAsync(int distributorId)
        {
            var distributor = await _db.Distributors.FirstOrDefaultAsync(x => x.Id == distributorId);
            if (distributor == null) return null;

            if (!string.IsNullOrWhiteSpace(distributor.ApplicationUserId))
                return distributor.ApplicationUserId;

            if (!string.IsNullOrWhiteSpace(distributor.Email))
            {
                var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == distributor.Email);
                if (user != null) return user.Id;
            }

            var fallback = await _db.Users.FirstOrDefaultAsync(x => x.Email == "distributor@bevera.local");
            return fallback?.Id;
        }
    }
}