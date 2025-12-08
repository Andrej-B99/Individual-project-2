using Microsoft.AspNetCore.Mvc;

namespace MasterServicePlatform.Web.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
