using Skoruba.IdentityServer4.Shared.Configuration.Configuration.Identity;

namespace SingleSignOn.AuthServer.MVC.Configuration.Interfaces
{
    public interface IRootConfiguration
    {
        AdminConfiguration AdminConfiguration { get; }

        RegisterConfiguration RegisterConfiguration { get; }
    }
}







