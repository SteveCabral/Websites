using Microsoft.AspNetCore.Mvc;

namespace FamilyGameServer.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return Content("Hello World");
        }
    }
}