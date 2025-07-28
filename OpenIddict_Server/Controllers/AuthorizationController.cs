using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict_Server.Data;
using System.Security.Claims;

namespace OpenIddict_Server.Controllers
{
    [Route("connect")]
    public class AuthorizationController(IAppDbContextResolver resolver) : Controller
    {
        private readonly IAppDbContextResolver _resolver = resolver;

        [HttpGet("authorize")]
        public async Task<IActionResult> Authorize()
        {
            
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                var returnUrl = Request.Path + QueryString.Create(Request.Query);
                return Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
            }

            var appName = Request.Query["app_name"].ToString();
            
            ApplicationDbContext? _dbContext = _resolver.GetDbContext(appName);
            
            var username = User.Identity.Name;
            if (string.IsNullOrEmpty(username))
                return Forbid(); 
            
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Forbid(); 
            
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var subjectClaim = new Claim(OpenIddictConstants.Claims.Subject, user.Id.ToString());
            subjectClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
            identity.AddClaim(subjectClaim);

            var nameClaim = new Claim(OpenIddictConstants.Claims.Name, user.Username);
            nameClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken);
            identity.AddClaim(nameClaim);

            var principal = new ClaimsPrincipal(identity);
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
