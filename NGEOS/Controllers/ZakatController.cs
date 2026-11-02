using Microsoft.AspNetCore.Mvc;

namespace NGEOS.Controllers
{
    public class ZakatController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Smart Zakat Calculator - NGEOS";
            return View();
        }
    }
}
