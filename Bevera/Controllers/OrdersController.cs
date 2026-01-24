using Bevera.Data;                  // <-- DbContext namespace
using Bevera.Extensions;
using Bevera.Models;                // <-- тук трябва да е Order
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin,Worker")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? status, string? q, int page = 1, int pageSize = 10)
        {
            IQueryable<Order> query = _context.Orders
                .Include(o => o.Client)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.Status == status);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(o =>
                    o.Id.ToString().Contains(q) ||
                    (o.Client.Email ?? "").Contains(q) ||
                    ((o.Client.FirstName ?? "") + " " + (o.Client.LastName ?? "")).Contains(q));
            }

            query = query.OrderByDescending(o => o.CreatedAt);

            var paged = await query.ToPagedAsync(page, pageSize);

            ViewBag.Status = status;
            ViewBag.Q = q;
            ViewBag.PageSize = pageSize;

            return View(paged);
        }
    }
}
