using Microsoft.EntityFrameworkCore;

namespace OpenIddict_Server.Data
{
    public class AppDbContextResolver : IAppDbContextResolver
    {
        private readonly IConfiguration _configuration;

        public AppDbContextResolver(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public ApplicationDbContext GetDbContext(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                throw new ArgumentNullException(nameof(appName));

            var connStr = _configuration.GetConnectionString(appName);

            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException($"No connection string found for app '{appName}'");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connStr);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }

}
