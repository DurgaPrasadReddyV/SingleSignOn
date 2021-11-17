using Skoruba.IdentityServer4.Shared.Configuration.Configuration.Identity;
using IdentityServer.AuthServer.Configuration.Interfaces;

namespace IdentityServer.AuthServer.Configuration
{
    public class RootConfiguration : IRootConfiguration
    {      
        public AdminConfiguration AdminConfiguration { get; } = new AdminConfiguration();
        public RegisterConfiguration RegisterConfiguration { get; } = new RegisterConfiguration();
    }
}







