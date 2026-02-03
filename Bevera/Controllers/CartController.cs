using Bevera.Data;
using Bevera.Models;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Client")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var items = await _db.CartItems
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                .AsNoTracking()
                .ToListAsync();

            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int qty = 1)
        {
            if (qty < 1) qty = 1;

            var userId = _userManager.GetUserId(User);
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var existing = await _db.CartItems.FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity += qty;
                existing.UnitPrice = product.Price;
            }
            else
            {
                _db.CartItems.Add(new CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = qty,
                    UnitPrice = product.Price
                });
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _db.CartItems.FirstOrDefaultAsync(x => x.Id == cartItemId && x.UserId == userId);
            if (item == null) return NotFound();

            if (quantity < 1) quantity = 1;
            item.Quantity = quantity;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var userId = _userManager.GetUserId(User);
            var item = await _db.CartItems.FirstOrDefaultAsync(x => x.Id == cartItemId && x.UserId == userId);
            if (item == null) return NotFound();

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var vm = new CheckoutViewModel
            {
                DeliveryAddress = user.Address ?? ""
            };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid) return View(vm);

            var cart = await _db.CartItems
                .Where(c => c.UserId == user.Id)
                .Include(c => c.Product)
                .ToListAsync();

            if (!cart.Any())
            {
                ModelState.AddModelError("", "Cart is empty.");
                return View(vm);
            }

            // ✅ Create order
            var order = new Order
            {
                ClientId = user.Id,
                Status = "Pending",
                ChangedAt = DateTime.UtcNow,
                Total = 0m
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            decimal total = 0m;
            foreach (var c in cart)
            {
                var unit = c.Product.Price;
                var line = unit * c.Quantity;
                total += line;

                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = c.ProductId,
                    Quantity = c.Quantity,
                    UnitPrice = unit,
                    LineTotal = line
                });
            }

            order.Total = total;

            _db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                Status = "Pending",
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = user.Id
            });

            // clear cart
            _db.CartItems.RemoveRange(cart);

            await _db.SaveChangesAsync();

            return RedirectToAction("Thanks", "Orders", new { id = order.Id });
        }
    }
}
