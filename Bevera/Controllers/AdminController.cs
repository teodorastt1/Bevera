using Bevera.Data;
using Bevera.Models;
using Bevera.Models.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // 1) Basic counts
            var categoriesCount = await _db.Categories.CountAsync();
            var productsCount = await _db.Products.CountAsync();

            // 2) Orders
            var ordersCount = await _db.Orders.CountAsync();

            // Ако държиш статусите като string:
            var pendingOrders = await _db.Orders.CountAsync(o => o.Status == "Pending");
            var deliveredOrders = await _db.Orders.CountAsync(o => o.Status == "Delivered");

            // 3) Revenue (избери логика)
            // Вариант A (най-логичен): само платени и доставени
            var revenue = await _db.Orders
                .Where(o => o.PaymentStatus == "Paid" && o.Status == "Delivered")
                .SumAsync(o => (decimal?)o.Total) ?? 0m;

            // 4) Stock indicators
            var lowStockProducts = await _db.Products
                .CountAsync(p => p.StockQty > 0 && p.StockQty <= p.LowStockThreshold);

            var outOfStockProducts = await _db.Products
                .CountAsync(p => p.StockQty <= 0);

            // 5) Users count
            // Ако искаш само клиенти:
            // (най-чисто е през roles, но това е по-тежко)
            // Засега: всички users минус admin/worker може да стане по-нататък.
            var usersCount = await _db.Users.CountAsync();


            var model = new AdminDashboardViewModel
            {
                CategoriesCount = categoriesCount,
                ProductsCount = productsCount,
                OrdersCount = ordersCount,
                PendingOrders = pendingOrders,
                DeliveredOrders = deliveredOrders,
                UsersCount = usersCount,
                Revenue = revenue,
                LowStockProducts = lowStockProducts,
               
            };

            return View(model);
        }
    }
}
