using Microsoft.AspNetCore.Mvc;

namespace PROG6212POE.Controllers
{
    public class ManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
