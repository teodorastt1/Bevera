using Bevera.Data;
using Bevera.Helpers;
using Bevera.Models.Catalog;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string CartKey = "CART";

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        private Dictionary<int, int> GetCart()
            => HttpContext.Session.GetObject<Dictionary<int, int>>(CartKey) ?? new Dictionary<int, int>();

        private void SaveCart(Dictionary<int, int> cart)
            => HttpContext.Session.SetObject(CartKey, cart);

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();
            var ids = cart.Keys.ToList();

            var products = await _db.Set<Product>()
                .Where(p => ids.Contains(p.Id))
                .Include(p => p.Images)
                .ToListAsync();

            var items = products.Select(p =>
            {
                var img = p.Images?.FirstOrDefault(i => i.IsMain)?.ImagePath
                          ?? p.Images?.FirstOrDefault()?.ImagePath
                          ?? "/images/image-1.jpg";

                return new CartItemVm
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    ImagePath = img,
                    UnitPrice = p.Price,
                    Quantity = cart[p.Id]
                };
            }).OrderBy(i => i.Name).ToList();

            ViewBag.GrandTotal = items.Sum(i => i.Total);
            return View(items); // Views/Cart/Index.cshtml
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int productId, int qty = 1, string? returnUrl = null)
        {
            if (qty < 1) qty = 1;

            var cart = GetCart();
            if (cart.ContainsKey(productId)) cart[productId] += qty;
            else cart[productId] = qty;

            SaveCart(cart);

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int productId, int qty)
        {
            var cart = GetCart();

            if (qty <= 0)
                cart.Remove(productId);
            else
                cart[productId] = qty;

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productId)
        {
            var cart = GetCart();
            cart.Remove(productId);
            SaveCart(cart);
            return RedirectToAction("Index");
        }
    }
}
