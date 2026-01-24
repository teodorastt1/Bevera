using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bevera.Controllers;

[Authorize(Roles = "Worker,Admin")]
public class WorkerController : Controller
{
    public IActionResult Index() => View();
}
