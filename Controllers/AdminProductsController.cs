using Bevera.Data;
using Bevera.Models.Catalog;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin,Worker")]
    public class AdminProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /AdminProducts
        public async Task<IActionResult> Index(
    string? q,
    int? categoryId,
    int? brandId,
    string? stock,
    string? active,
    string? sort,
    int? minQty,
    int? maxQty,
    int? minMl,
    int? maxMl,
    int page = 1)
        {
            const int pageSize = 15;

            if (page < 1)
                page = 1;

            ViewBag.Q = q;
            ViewBag.CategoryId = categoryId;
            ViewBag.BrandId = brandId;
            ViewBag.Stock = stock;
            ViewBag.Active = active;
            ViewBag.Sort = sort ?? "";
            ViewBag.PageSize = pageSize;
            ViewBag.MinQty = minQty?.ToString() ?? "";
            ViewBag.MaxQty = maxQty?.ToString() ?? "";
            ViewBag.MinMl = minMl?.ToString() ?? "";
            ViewBag.MaxMl = maxMl?.ToString() ?? "";

            ViewBag.Categories = await _context.Categories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .Where(c => c.ParentCategoryId != null)
                .OrderBy(c => c.ParentCategory!.Name)
                .ThenBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.ParentCategory!.Name} → {c.Name}"
                })
                .ToListAsync();

            ViewBag.Brands = await _context.Brands
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Name
                })
                .ToListAsync();

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var search = q.Trim();

                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Description != null && p.Description.Contains(search)) ||
                    (p.SKU != null && p.SKU.Contains(search)));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (brandId.HasValue && brandId.Value > 0)
                query = query.Where(p => p.BrandId == brandId.Value);

            if (stock == "low")
            {
                query = query.Where(p =>
                    p.StockQty > 0 &&
                    p.StockQty <= (p.LowStockThreshold > 0 ? p.LowStockThreshold : 5));
            }
            else if (stock == "out")
            {
                query = query.Where(p => p.StockQty <= 0);
            }
            else if (stock == "in")
            {
                query = query.Where(p => p.StockQty > 0);
            }

            if (active == "yes")
                query = query.Where(p => p.IsActive);
            else if (active == "no")
                query = query.Where(p => !p.IsActive);

            if (minQty.HasValue)
                query = query.Where(p => p.StockQty >= minQty.Value);

            if (maxQty.HasValue)
                query = query.Where(p => p.StockQty <= maxQty.Value);

            if (minMl.HasValue)
                query = query.Where(p => p.Ml.HasValue && p.Ml.Value >= minMl.Value);

            if (maxMl.HasValue)
                query = query.Where(p => p.Ml.HasValue && p.Ml.Value <= maxMl.Value);

            query = sort switch
            {
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),

                "price_asc" => query.OrderBy(p => p.Price).ThenBy(p => p.Name),
                "price_desc" => query.OrderByDescending(p => p.Price).ThenBy(p => p.Name),

                "cost_asc" => query.OrderBy(p => p.CostPrice).ThenBy(p => p.Name),
                "cost_desc" => query.OrderByDescending(p => p.CostPrice).ThenBy(p => p.Name),

                "stock_asc" => query.OrderBy(p => p.StockQty).ThenBy(p => p.Name),
                "stock_desc" => query.OrderByDescending(p => p.StockQty).ThenBy(p => p.Name),

                "ml_asc" => query.OrderBy(p => p.Ml).ThenBy(p => p.Name),
                "ml_desc" => query.OrderByDescending(p => p.Ml).ThenBy(p => p.Name),

                _ => query.OrderByDescending(p => p.Id)
            };

            var total = await query.CountAsync();

            var totalPages = pageSize <= 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);

            if (totalPages > 0 && page > totalPages)
                page = totalPages;

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminProductRowViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU,
                    CategoryName = p.Category.Name,
                    BrandName = p.Brand != null ? p.Brand.Name : null,
                    Price = p.Price,
                    CostPrice = p.CostPrice,
                    StockQty = p.StockQty,
                    LowStockThreshold = p.LowStockThreshold,
                    Ml = p.Ml,
                    PackageType = p.PackageType,
                    DiscountPercent = p.DiscountPercent,
                    DiscountEndsAt = p.DiscountEndsAt,
                    IsActive = p.IsActive,
                    ImagePath = p.Images
                        .OrderByDescending(i => i.IsMain)
                        .Select(i => i.ImagePath)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var model = new PagedResult<AdminProductRowViewModel>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            };

            return View(model);
        }
        // GET: /AdminProducts/Create
        public async Task<IActionResult> Create()
        {
            var vm = new AdminProductFormViewModel
            {
                Categories = await SubCategoriesDropDown(),
                Brands = await BrandsDropDown(),
                IsActive = true,
                LowStockThreshold = 5,
                StockQty = 0
            };

            return View(vm);
        }

        // POST: /AdminProducts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminProductFormViewModel vm)
        {
            await ValidateProductForm(vm);

            if (!ModelState.IsValid)
            {
                vm.Categories = await SubCategoriesDropDown();
                vm.Brands = await BrandsDropDown();
                return View(vm);
            }

            var product = new Product
            {
                Name = vm.Name.Trim(),
                SKU = string.IsNullOrWhiteSpace(vm.SKU) ? null : vm.SKU.Trim(),
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),

                Price = vm.Price,
                CostPrice = vm.CostPrice,

                CategoryId = vm.CategoryId,
                BrandId = vm.BrandId,

                StockQty = 0,
                LowStockThreshold = vm.LowStockThreshold,

                Ml = vm.Ml,
                PackageType = string.IsNullOrWhiteSpace(vm.PackageType) ? null : vm.PackageType.Trim(),

                DiscountPercent = NormalizeDiscount(vm.DiscountPercent),
                DiscountEndsAt = vm.DiscountEndsAt,

                IsActive = vm.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var savedPath = await SaveProductImage(vm.ImageFile);

                var img = new ProductImage
                {
                    ProductId = product.Id,
                    ImagePath = savedPath,
                    IsMain = true
                };

                _context.ProductImages.Add(img);
                await _context.SaveChangesAsync();
            }

            TempData["ToastMessage"] = "Продуктът е добавен успешно.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        // GET: /AdminProducts/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var vm = new AdminProductFormViewModel
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Description = product.Description,

                Price = product.Price,
                CostPrice = product.CostPrice,

                CategoryId = product.CategoryId,
                BrandId = product.BrandId,

                StockQty = product.StockQty,
                LowStockThreshold = product.LowStockThreshold,

                Ml = product.Ml ?? 0,
                PackageType = product.PackageType,

                DiscountPercent = product.DiscountPercent,
                DiscountEndsAt = product.DiscountEndsAt,

                IsActive = product.IsActive,

                ExistingImagePath = product.Images
                    .OrderByDescending(i => i.IsMain)
                    .Select(i => i.ImagePath)
                    .FirstOrDefault(),

                Categories = await SubCategoriesDropDown(),
                Brands = await BrandsDropDown()
            };

            return View(vm);
        }

        // POST: /AdminProducts/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminProductFormViewModel vm)
        {
            await ValidateProductForm(vm);

            if (!ModelState.IsValid)
            {
                vm.Categories = await SubCategoriesDropDown();
                vm.Brands = await BrandsDropDown();
                return View(vm);
            }

            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == vm.Id);

            if (product == null) return NotFound();

            product.Name = vm.Name.Trim();
            product.SKU = string.IsNullOrWhiteSpace(vm.SKU) ? null : vm.SKU.Trim();
            product.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();

            product.Price = vm.Price;
            product.CostPrice = vm.CostPrice;

            product.CategoryId = vm.CategoryId;
            product.BrandId = vm.BrandId;

            // Админът не задава наличност ръчно. Тя се контролира от доставки/продажби.
            product.LowStockThreshold = vm.LowStockThreshold;

            product.Ml = vm.Ml;
            product.PackageType = string.IsNullOrWhiteSpace(vm.PackageType) ? null : vm.PackageType.Trim();

            product.DiscountPercent = NormalizeDiscount(vm.DiscountPercent);
            product.DiscountEndsAt = vm.DiscountEndsAt;

            product.IsActive = vm.IsActive;

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                var oldMain = product.Images.FirstOrDefault(i => i.IsMain);
                if (oldMain != null)
                {
                    DeletePhysicalFileIfExists(oldMain.ImagePath);
                    oldMain.IsMain = false;
                }

                var saved = await SaveProductImage(vm.ImageFile);

                var newImg = new ProductImage
                {
                    ProductId = product.Id,
                    ImagePath = saved,
                    IsMain = true
                };

                product.Images.Add(newImg);
            }

            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Продуктът е редактиран успешно.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Index));
        }


        // GET: /AdminProducts/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // POST: /AdminProducts/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var hasInventoryMovements = await _context.InventoryMovements.AnyAsync(x => x.ProductId == id);
            var hasOrderItems = await _context.OrderItems.AnyAsync(x => x.ProductId == id);
            var hasReviews = await _context.Reviews.AnyAsync(x => x.ProductId == id);
            var hasFavorites = await _context.Favorites.AnyAsync(x => x.ProductId == id);
            var hasDistributorProducts = await _context.DistributorProducts.AnyAsync(x => x.ProductId == id);
            var hasPurchaseOrderItems = await _context.PurchaseOrderItems.AnyAsync(x => x.ProductId == id);

            if (hasInventoryMovements || hasOrderItems || hasReviews || hasFavorites || hasDistributorProducts || hasPurchaseOrderItems)
            {
                product.IsActive = false;
                await _context.SaveChangesAsync();

                TempData["ToastMessage"] = "Продуктът участва в история и не беше изтрит физически. Маркиран е като неактивен.";
                TempData["ToastType"] = "warning";
                return RedirectToAction(nameof(Index));
            }

            foreach (var img in product.Images.ToList())
            {
                DeletePhysicalFileIfExists(img.ImagePath);
            }

            if (product.Images.Any())
                _context.ProductImages.RemoveRange(product.Images);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Продуктът е изтрит успешно.";
            TempData["ToastType"] = "success";

            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateProductForm(AdminProductFormViewModel vm)
        {
            if (!await IsSubCategory(vm.CategoryId))
            {
                ModelState.AddModelError(nameof(vm.CategoryId), "Продукт може да бъде добавен само към подкатегория.");
            }

            if (vm.Price < 0)
            {
                ModelState.AddModelError(nameof(vm.Price), "Продажната цена не може да е отрицателна.");
            }

            if (vm.CostPrice < 0)
            {
                ModelState.AddModelError(nameof(vm.CostPrice), "Доставната цена не може да е отрицателна.");
            }

            if (vm.StockQty < 0)
            {
                ModelState.AddModelError(nameof(vm.StockQty), "Наличността не може да е отрицателна.");
            }

            if (vm.LowStockThreshold < 0)
            {
                ModelState.AddModelError(nameof(vm.LowStockThreshold), "Прагът за ниска наличност не може да е отрицателен.");
            }

            if (vm.Ml <= 0)
            {
                ModelState.AddModelError(nameof(vm.Ml), "Моля, изберете милилитри.");
            }

            if (vm.DiscountPercent.HasValue)
            {
                if (vm.DiscountPercent.Value < 0 || vm.DiscountPercent.Value > 90)
                {
                    ModelState.AddModelError(nameof(vm.DiscountPercent), "Отстъпката трябва да е между 0 и 90%.");
                }

                if (vm.DiscountPercent.Value > 0 && vm.DiscountEndsAt.HasValue && vm.DiscountEndsAt.Value < DateTime.UtcNow)
                {
                    ModelState.AddModelError(nameof(vm.DiscountEndsAt), "Краят на промоцията не може да е в миналото.");
                }
            }

            if (!string.IsNullOrWhiteSpace(vm.SKU))
            {
                var normalizedSku = vm.SKU.Trim();

                var skuExists = await _context.Products.AnyAsync(p =>
                    p.SKU != null &&
                    p.SKU == normalizedSku &&
                    p.Id != vm.Id);

                if (skuExists)
                {
                    ModelState.AddModelError(nameof(vm.SKU), "Вече съществува продукт с този SKU.");
                }
            }
        }

        private decimal? NormalizeDiscount(decimal? discountPercent)
        {
            if (!discountPercent.HasValue || discountPercent.Value <= 0)
                return null;

            return discountPercent.Value;
        }

        private async Task<List<SelectListItem>> SubCategoriesDropDown()
        {
            var subs = await _context.Categories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .Where(c => c.IsActive && c.ParentCategoryId != null)
                .OrderBy(c => c.ParentCategory!.Name)
                .ThenBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.ParentCategory!.Name} → {c.Name}"
                })
                .ToListAsync();

            subs.Insert(0, new SelectListItem { Value = "", Text = "— Избери подкатегория —" });
            return subs;
        }

        private async Task<List<SelectListItem>> BrandsDropDown()
        {
            var brands = await _context.Brands
                .AsNoTracking()
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.Name
                })
                .ToListAsync();

            brands.Insert(0, new SelectListItem { Value = "", Text = "— Без марка —" });
            return brands;
        }

        private async Task<bool> IsSubCategory(int categoryId)
        {
            return await _context.Categories
                .AsNoTracking()
                .AnyAsync(c => c.Id == categoryId && c.ParentCategoryId != null);
        }

        private async Task<string> SaveProductImage(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName);
            var name = $"prod_{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/products/{name}";
        }

        private void DeletePhysicalFileIfExists(string? publicPath)
        {
            if (string.IsNullOrWhiteSpace(publicPath)) return;

            var relative = publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relative);

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}