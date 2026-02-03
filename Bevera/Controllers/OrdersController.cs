using Bevera.Data;
using Bevera.Extensions;
using Bevera.Models;
using Bevera.Models.ViewModels;
using Bevera.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Admin,Worker")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly InvoiceService _invoiceService;

        public OrdersController(ApplicationDbContext context, InvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
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

            query = query.OrderByDescending(o => o.ChangedAt);

            var paged = await query.ToPagedAsync(page, pageSize);

            ViewBag.Status = status;
            ViewBag.Q = q;
            ViewBag.PageSize = pageSize;

            return View(paged);
        }

        [Authorize(Roles = "Admin,Worker")]
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            // If invoice metadata is missing, generate it.
            // InvoiceService saves metadata to the database itself.
            if (string.IsNullOrEmpty(order.InvoiceStoredFileName))
            {
                await _invoiceService.GenerateInvoiceAsync(order.Id);

                // Reload order to ensure we have the updated metadata.
                order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
                if (order == null) return NotFound();

                if (string.IsNullOrEmpty(order.InvoiceStoredFileName))
                    return StatusCode(500, "Failed to generate invoice metadata.");
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "invoices", order.InvoiceStoredFileName);

            if (!System.IO.File.Exists(path))
                return NotFound("Фактурата липсва на сървъра.");

            return PhysicalFile(path, order.InvoiceContentType ?? "application/pdf", order.InvoiceFileName ?? $"Invoice_{order.Id}.pdf");
        }

    }
}