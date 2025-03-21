using Microsoft.AspNetCore.Mvc;

namespace HRManagement.Controllers
{
    public class RemoveMessageController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public IActionResult ClearMessage()
        {
            ViewBag.Message = null;
            return Ok();
        }
    }
}
