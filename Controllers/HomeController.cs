using Microsoft.AspNetCore.Mvc;

namespace FarmFlow.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Redirect to login page
        return RedirectToAction("Login", "Auth");
    }

    public IActionResult Privacy()
    {
        return View();
    }
}