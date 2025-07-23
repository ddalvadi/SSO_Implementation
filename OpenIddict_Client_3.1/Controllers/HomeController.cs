using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenIddict_Client_3._1.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace OpenIddict_Client_3._1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = returnUrl }, "oidc");
        }

        public IActionResult Logout()
        {
            var request = HttpContext.Request;

            var clientBaseUrl = $"{request.Scheme}://{request.Host}/";
            var authServerBaseUrl = "https://sso.localtest.me:7217";
            var postLogoutRedirectUri = $"{clientBaseUrl}signout-callback-oidc";
            var logoutRedirectUri = $"{authServerBaseUrl}/connect/logout?post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";

            return SignOut(new AuthenticationProperties
            {
                RedirectUri = logoutRedirectUri
            },
            CookieAuthenticationDefaults.AuthenticationScheme,
            "oidc"); 
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
