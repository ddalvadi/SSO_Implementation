using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict_Server.Data;
using System.Security.Claims;
using System.Web;

namespace OpenIddict_Server.Controllers
{
    public class AccountController(IAppDbContextResolver resolver) : Controller
    {
        private readonly IAppDbContextResolver _resolver = resolver;

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            var query = HttpUtility.ParseQueryString(returnUrl);
            var appName = query["app_name"];

            var appDb = _resolver.GetDbContext(appName);

            var user = await appDb.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Password == password && !u.IsDeleted && u.IsActive);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.Username),
                    new("UserId", user.Id.ToString())
                };

                var identity = new ClaimsIdentity(claims, "Cookies");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("Cookies", principal);

                //await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties
                //{
                //    IsPersistent = false
                //});


                return Redirect(returnUrl ?? "/");
            }

            ModelState.AddModelError("", "Invalid credentials");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }


        [AcceptVerbs("GET", "POST")]
        [Route("~/connect/logout")]
        public IActionResult Logout()
        {
            return SignOut(
                new AuthenticationProperties
                {
                    RedirectUri = "/"
                },
                "Cookies",
                OpenIddict.Server.AspNetCore.OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
            );
        }

    }

}
