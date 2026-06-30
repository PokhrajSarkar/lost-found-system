using LostFound.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace LostFound.Controllers
{
    public class HomeController : Controller
    {
        private readonly LostFoundContext _context;
        private readonly UserManager<User> _userManager;

        public HomeController(LostFoundContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            ViewBag.TotalItems = _context.Items.Count();
            ViewBag.TotalMatches = _context.Items.Count(i => i.RelatedItemId != null && i.Status == ItemStatus.Resolved);
            ViewBag.TotalUsers = _userManager.Users.Count();
            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
