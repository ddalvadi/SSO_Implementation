using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;

namespace OpenIddict_Server.Controllers
{
    [Route("connect")]
    public class AuthorizationController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public AuthorizationController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet("authorize")]
        public async Task<IActionResult> Authorize()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                var returnUrl = Request.Path + QueryString.Create(Request.Query);
                return Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            var user = await _userManager.GetUserAsync(User);
            var principal = await _signInManager.CreateUserPrincipalAsync(user);

            var identity = (ClaimsIdentity)principal.Identity!;
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Id)
                .SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.UserName)
                .SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken));

            principal.SetScopes("openid", "profile");
            principal.SetResources("resource_server");

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }


        [HttpPost("~/connect/token")]
        [Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest()
                ?? throw new InvalidOperationException();

            if (request.IsAuthorizationCodeGrantType())
            {
                var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

                var principal = result.Principal ?? throw new InvalidOperationException();
                var identity = (ClaimsIdentity)principal.Identity!;

                if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.Subject))
                {
                    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.NewGuid().ToString())
                        .SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken));
                }

                if (!identity.HasClaim(c => c.Type == OpenIddictConstants.Claims.Name))
                {
                    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, identity.Name)
                        .SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken));
                }

                return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            throw new InvalidOperationException("The specified grant type is not supported.");
        }


    }
}
