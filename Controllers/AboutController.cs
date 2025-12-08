using Microsoft.AspNetCore.Mvc;

namespace MasterServicePlatform.Web.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
