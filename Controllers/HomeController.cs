using Microsoft.AspNetCore.Mvc;

namespace TUVACA.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
