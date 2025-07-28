using System;

namespace OpenIddict_Server.Data
{
    public interface IAppDbContextResolver
    {
        ApplicationDbContext GetDbContext(string appName);
    }
}
