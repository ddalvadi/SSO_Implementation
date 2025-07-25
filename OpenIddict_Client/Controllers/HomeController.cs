using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using OidcClientApp.Models;
using System.Diagnostics;

namespace OidcClientApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;


    public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Login()
    {
        return Challenge(new AuthenticationProperties { RedirectUri = "/" });
    }

    public IActionResult Logout()
    {
        var request = HttpContext.Request;

        var clientBaseUrl = $"{request.Scheme}://{request.Host}/";

        var authServerBaseUrl = _configuration["OpenIddict:Authority"]
            ?? "https://localhost:7217";

        var postLogoutRedirectUri = $"{clientBaseUrl}signout-callback-oidc";

        var logoutRedirectUri = $"{authServerBaseUrl}/connect/logout?post_logout_redirect_uri={Uri.EscapeDataString(postLogoutRedirectUri)}";

        return SignOut(new AuthenticationProperties
        {
            RedirectUri = logoutRedirectUri
        },
        CookieAuthenticationDefaults.AuthenticationScheme,
        OpenIdConnectDefaults.AuthenticationScheme);
    }



    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}