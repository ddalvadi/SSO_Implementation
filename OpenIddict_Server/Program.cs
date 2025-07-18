using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict_Server.Data;

var builder = WebApplication.CreateBuilder(args);

// 🔧 EF Core with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.UseOpenIddict(); // Required for OpenIddict entity support
});

// 🧩 Identity setup
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 🔐 OpenIddict setup
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        // OIDC-compliant endpoints
        options.SetConfigurationEndpointUris("/.well-known/openid-configuration");
        options.SetAuthorizationEndpointUris("/connect/authorize");
        options.SetTokenEndpointUris("/connect/token");
        options.SetEndSessionEndpointUris("connect/logout");

        // 🔐 Required: certs
        options.AddDevelopmentEncryptionCertificate();
        options.AddDevelopmentSigningCertificate();

        // 🔑 Auth code flow
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange();

        // 🔎 Scopes (OIDC-compliant)
        options.RegisterScopes("openid", "profile", "email");

        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// 🧾 Swagger + API services
//builder.Services.AddControllers();
builder.Services.AddControllersWithViews(); // ✅ enables support for Razor views

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


// 📦 Seed database and client app
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();

    var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

    // 🔄 Delete old client if exists
    var descriptor = await manager.FindByClientIdAsync("test_client");
    if (descriptor is not null)
    {
        await manager.DeleteAsync(descriptor);
    }

    // ✅ Create fresh client with correct redirect URI
    await manager.CreateAsync(new OpenIddictApplicationDescriptor
    {
        ClientId = "test_client",
        ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
        DisplayName = "Test Client App",
        RedirectUris = { new Uri("https://localhost:7192/signin-oidc") },
        PostLogoutRedirectUris =
        {
            //new Uri("https://localhost:7192/")
            new Uri("https://localhost:7192/signout-callback-oidc")
        },
        Permissions =
        {
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.Endpoints.EndSession,
            //OpenIddictConstants.Permissions.Endpoints.Logout,
            "ept:logout",
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
    });
}

// ✅ Middleware
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultControllerRoute(); 

// ✅ Optional test route for root URL
app.MapGet("/", () => "✅ OpenIddict Authorization Server is running!");


app.Run();
