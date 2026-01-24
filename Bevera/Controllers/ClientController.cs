using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bevera.Controllers;

[Authorize(Roles = "Client,Admin")]
public class ClientController : Controller
{
    public IActionResult Index() => View();
}
