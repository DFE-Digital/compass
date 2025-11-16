using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers;

[Authorize]
public class PeopleSearchController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}

