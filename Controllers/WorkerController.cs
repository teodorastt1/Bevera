using Bevera.Data;
using Bevera.Helpers;
using Bevera.Models;
using Bevera.Models.Inventory;
using Bevera.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bevera.Controllers
{
    [Authorize(Roles = "Worker")]
    public class WorkerController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public WorkerController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // =========================
        // DASHBOARD
        // =========================
        public async Task<IActionResult> Index()
        {
            var vm = new WorkerDashboardVm
            {
                NewOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.Submitted),
                PreparingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.Preparing),
                ShippedOrders = await _db.Orders.CountAsync(o => o.Status == OrderStates.ReadyForPickup),
                AwaitingPayment = await _db.Orders.CountAsync(o => o.PaymentStatus == PaymentStates.Unpaid),
                Paid = await _db.Orders.CountAsync(o => o.PaymentStatus == PaymentStates.Paid)
            };

            return View(vm);
        }

       

      


    }
}
