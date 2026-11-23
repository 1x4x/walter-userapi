using Microsoft.AspNetCore.Mvc;
namespace AnonymousUserMvcApi.Controllers;
public sealed class HomeController : Controller
{
    public IActionResult Index() => View();
}