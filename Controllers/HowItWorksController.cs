using Microsoft.AspNetCore.Mvc;

namespace MasterServicePlatform.Web.Controllers
{
    public class HowItWorksController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
