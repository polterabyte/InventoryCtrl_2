using Microsoft.AspNetCore.Mvc;

namespace Inventory.Server.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
