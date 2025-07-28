using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict_Server.Models;

namespace OpenIddict_Server.Data
{
    public class ApplicationDbContext : DbContext, IDataProtectionKeyContext //IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.UseOpenIddict();
        }

        public DbSet<AppRedirectUri> AppRedirectUris { get; set; }
        public DbSet<Users> Users { get; set; }

        public DbSet<Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey> DataProtectionKeys { get; set; }

    }
}