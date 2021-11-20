using Skoruba.IdentityServer4.Shared.Configuration.Configuration.Identity;
using SingleSignOn.AuthServer.MVC.Configuration.Interfaces;

namespace SingleSignOn.AuthServer.MVC.Configuration
{
    public class RootConfiguration : IRootConfiguration
    {      
        public AdminConfiguration AdminConfiguration { get; } = new AdminConfiguration();
        public RegisterConfiguration RegisterConfiguration { get; } = new RegisterConfiguration();
    }
}







