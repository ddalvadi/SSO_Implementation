using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict_Server.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("SSO-OpenIddict");

builder.Services.AddScoped<IAppDbContextResolver, AppDbContextResolver>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultAuthenticateScheme = "Cookies";
    options.DefaultSignInScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.Name = "auth_cookie";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = false;
    options.Cookie.Domain = ".localtest.me";
    options.Events.OnSigningIn = context =>
    {
        context.Properties.IsPersistent = false; // 🔥 Ensures session-only cookie
        return Task.CompletedTask;
    };
});

// 🔐 OpenIddict setup
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetConfigurationEndpointUris("/.well-known/openid-configuration");
        options.SetAuthorizationEndpointUris("/connect/authorize");
        options.SetTokenEndpointUris("/connect/token");
        options.SetEndSessionEndpointUris("/connect/logout");

        options.AddDevelopmentEncryptionCertificate();
        options.AddDevelopmentSigningCertificate();
        options.DisableAccessTokenEncryption();

        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();

        options.RegisterScopes("openid", "profile", "email");

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    Console.WriteLine("Block executed");

    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();

    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    var findClient = await manager.FindByClientIdAsync("datasos_client");
    if (findClient is not null)
    {
        await manager.DeleteAsync(findClient);
    }

    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    var redirectUris = dbContext.AppRedirectUris
        .Where(x => x.ClientId == "datasos_client")
        .Select(x => new Uri(x.RedirectUri))
        .ToList();

    var postLogoutUris = dbContext.AppRedirectUris
        .Where(x => x.ClientId == "datasos_client")
        .Select(x => new Uri(x.PostLogoutRedirectUri))
        .ToList();

    var descriptor = new OpenIddictApplicationDescriptor
    {
        ClientId = builder.Configuration["OpenIddict:ClientId"],
        ClientSecret = builder.Configuration["OpenIddict:ClientSecret"],
        ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
        Permissions =
        {
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.Endpoints.EndSession,
            //"ept:logout",
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            OpenIddictConstants.Permissions.ResponseTypes.Code,
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Profile,
            "scp:openId"
        },
        Requirements =
        {
            OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
        }
    };

    foreach (var uri in redirectUris)
        descriptor.RedirectUris.Add(uri);

    foreach (var uri in postLogoutUris)
        descriptor.PostLogoutRedirectUris.Add(uri);

    await manager.CreateAsync(descriptor);
}

app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute();

app.Run();
