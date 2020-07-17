using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace PSNotes.Controllers
{
    public class HomeController : Controller
    {
        private ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            _logger.LogInformation("Loading Home page");
            return View();
        }

        [Authorize]
        public IActionResult About()
        {
            _logger.LogInformation("Loading About page");

            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Error()
        {
            _logger.LogInformation("Loading Error page");

            return View();
        }
    }
}
