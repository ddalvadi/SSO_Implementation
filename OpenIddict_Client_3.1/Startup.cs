using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict_Client_3._1.DBOperations;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace OpenIddict_Client_3._1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddDataProtection()
            //    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\SharedKeys"))
            //    .SetApplicationName("SSO-OpenIddict");

            services.AddSingleton<IXmlRepository>(serviceProvider =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");

                return new DapperXmlRepository(
                    () => new SqlConnection(connectionString),
                    serviceProvider.GetRequiredService<ILogger<DapperXmlRepository>>());
            });

            services.AddDataProtection()
                .SetApplicationName("SSO-OpenIddict")
                .AddKeyManagementOptions(options =>
                {
                    var xmlRepo = services.BuildServiceProvider().GetRequiredService<IXmlRepository>();
                    options.XmlRepository = xmlRepo;
                });


            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
            //.AddCookie("Cookies")
            .AddCookie("Cookies", CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.IsEssential = true;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Name = "auth_cookie";
                options.Cookie.Domain = ".localtest.me";
                options.SlidingExpiration = false;
            })
            .AddOpenIdConnect("oidc", options =>
            {
                options.Authority = Configuration["OpenIddict:Authority"]; // Server URL
                options.ClientId = Configuration["OpenIddict:ClientId"];
                options.ClientSecret = Configuration["OpenIddict:ClientSecret"];
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters.NameClaimType = "name";
                options.BackchannelHttpHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                options.Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = context =>
                    {
                        var appName = "NJApp";
                        context.ProtocolMessage.SetParameter("app_name", appName);

                        return Task.CompletedTask;
                    },

                    OnSignedOutCallbackRedirect = context =>
                    {
                        context.HandleResponse();
                        context.Response.Redirect("/");
                        return Task.CompletedTask;
                    },

                    OnTokenValidated = context =>
                    {
                        // Explicitly override persistent flag again here (defensive)
                        context.Properties.IsPersistent = false;
                        return Task.CompletedTask;
                    }
                };

            });
            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
