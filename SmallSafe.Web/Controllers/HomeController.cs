using System.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallSafe.Web.ViewModels.Home;

namespace SmallSafe.Web.Controllers;

public class HomeController : Controller
{
    [Authorize]
    [HttpGet("~/")]
    public IActionResult Index()
    {           
        if (User == null)
            return Redirect("~/signin");

        return View(new IndexViewModel(HttpContext));
    }

    public IActionResult Error() => View(new ErrorViewModel(HttpContext));

    [HttpGet("~/signin")]
    public IActionResult Signin([FromQuery] string returnUrl) => View("Login", new LoginViewModel(HttpContext, returnUrl));

    [HttpPost("~/signin")]
    [ValidateAntiForgeryToken]
    public IActionResult SigninChallenge([FromForm] string returnUrl) => Challenge(new AuthenticationProperties { RedirectUri = $"/signedin?returnUrl={HttpUtility.UrlEncode(returnUrl)}" }, OpenIdConnectDefaults.AuthenticationScheme);

    [Authorize]
    [HttpGet("~/signedin")]
    public IActionResult SignedIn([FromQuery] string returnUrl) => Redirect("~/");

    [HttpPost("~/signout")]
    [ValidateAntiForgeryToken]
    public IActionResult Signout()
    {
        HttpContext.Session.Clear();
        return SignOut(CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }
}