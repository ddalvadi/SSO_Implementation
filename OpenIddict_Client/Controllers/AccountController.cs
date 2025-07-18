using Microsoft.AspNetCore.Mvc;

namespace OidcClientApp.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet("signout-callback-oidc")]
        public IActionResult SignOutCallback()
        {
            return RedirectToAction("Index", "Home"); // or return a view
        }
    }
}
