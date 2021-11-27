using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using SingleSignOn.EntityFrameworkCore.Entities;
using Skoruba.AuditLogging.EntityFramework.DbContexts;
using Skoruba.AuditLogging.EntityFramework.Entities;
using Skoruba.IdentityServer4.Admin.EntityFramework.Configuration.Configuration;
using Skoruba.IdentityServer4.Admin.EntityFramework.Interfaces;

public class SampleData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using (var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IdentityServerConfigurationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserIdentity>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<UserIdentityRole>>();
                var idsDataConfiguration = scope.ServiceProvider.GetRequiredService<IdentityServerData>();
                var idDataConfiguration = scope.ServiceProvider.GetRequiredService<IdentityData>();

                await EnsureSeedIdentityServerData(context, idsDataConfiguration);
                await EnsureSeedIdentityData(userManager, roleManager, idDataConfiguration);
            }
    }

    /// <summary>
        /// Generate default admin user / role
        /// </summary>
        private static async Task EnsureSeedIdentityData<TUser, TRole>(UserManager<TUser> userManager,
            RoleManager<TRole> roleManager, IdentityData identityDataConfiguration)
            where TUser : IdentityUser, new()
            where TRole : IdentityRole, new()
        {
            // adding roles from seed
            foreach (var r in identityDataConfiguration.Roles)
            {
                if (!await roleManager.RoleExistsAsync(r.Name))
                {
                    var role = new TRole
                    {
                        Name = r.Name
                    };

                    var result = await roleManager.CreateAsync(role);

                    if (result.Succeeded)
                    {
                        foreach (var claim in r.Claims)
                        {
                            await roleManager.AddClaimAsync(role, new System.Security.Claims.Claim(claim.Type, claim.Value));
                        }
                    }
                }
            }

            // adding users from seed
            foreach (var user in identityDataConfiguration.Users)
            {
                var identityUser = new TUser
                {
                    UserName = user.Username,
                    Email = user.Email,
                    EmailConfirmed = true
                };

                var userByUserName = await userManager.FindByNameAsync(user.Username);
                var userByEmail = await userManager.FindByEmailAsync(user.Email);

                // User is already exists in database
                if (userByUserName != default || userByEmail != default)
                {
                    continue;
                }

                // if there is no password we create user without password
                // user can reset password later, because accounts have EmailConfirmed set to true
                var result = !string.IsNullOrEmpty(user.Password)
                ? await userManager.CreateAsync(identityUser, user.Password)
                : await userManager.CreateAsync(identityUser);

                if (result.Succeeded)
                {
                    foreach (var claim in user.Claims)
                    {
                        await userManager.AddClaimAsync(identityUser, new System.Security.Claims.Claim(claim.Type, claim.Value));
                    }

                    foreach (var role in user.Roles)
                    {
                        await userManager.AddToRoleAsync(identityUser, role);
                    }
                }
            }
        }

        /// <summary>
        /// Generate default clients, identity and api resources
        /// </summary>
        private static async Task EnsureSeedIdentityServerData<TIdentityServerDbContext>(TIdentityServerDbContext context, IdentityServerData identityServerDataConfiguration)
            where TIdentityServerDbContext : DbContext, IAdminConfigurationDbContext
        {
            foreach (var resource in identityServerDataConfiguration.IdentityResources)
            {
                var exits = await context.IdentityResources.AnyAsync(a => a.Name == resource.Name);

                if (exits)
                {
                    continue;
                }

                await context.IdentityResources.AddAsync(resource.ToEntity());
            }

            foreach (var apiScope in identityServerDataConfiguration.ApiScopes)
            {
                var exits = await context.ApiScopes.AnyAsync(a => a.Name == apiScope.Name);

                if (exits)
                {
                    continue;
                }

                await context.ApiScopes.AddAsync(apiScope.ToEntity());
            }

            foreach (var resource in identityServerDataConfiguration.ApiResources)
            {
                var exits = await context.ApiResources.AnyAsync(a => a.Name == resource.Name);

                if (exits)
                {
                    continue;
                }

                foreach (var s in resource.ApiSecrets)
                {
                    s.Value = s.Value.ToSha256();
                }

                await context.ApiResources.AddAsync(resource.ToEntity());
            }


            foreach (var client in identityServerDataConfiguration.Clients)
            {
                var exits = await context.Clients.AnyAsync(a => a.ClientId == client.ClientId);

                if (exits)
                {
                    continue;
                }

                foreach (var secret in client.ClientSecrets)
                {
                    secret.Value = secret.Value.ToSha256();
                }

                client.Claims = client.ClientClaims
                    .Select(c => new ClientClaim(c.Type, c.Value))
                    .ToList();

                await context.Clients.AddAsync(client.ToEntity());
            }

            await context.SaveChangesAsync();
        }
}