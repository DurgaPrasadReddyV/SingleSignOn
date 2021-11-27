using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SingleSignOn.EntityFrameworkCore.Constants;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using SingleSignOn.EntityFrameworkCore.Entities;
using System;
using System.IO;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using System.Reflection;
using Skoruba.IdentityServer4.Admin.EntityFramework.Configuration.Configuration;

namespace SingleSignOn.DbMigrator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var serviceProvider = BuildServiceProvider(args);
            await MigrateDatabases(serviceProvider);
            await SampleData.Initialize(serviceProvider);
        }

        static async Task MigrateDatabases (IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using (var context = scope.ServiceProvider.GetRequiredService<IdentityServerPersistedGrantDbContext>())
                {
                    await context.Database.MigrateAsync();
                }

                using (var context = scope.ServiceProvider.GetRequiredService<UserIdentityDbContext>())
                {
                    await context.Database.MigrateAsync();
                }

                using (var context = scope.ServiceProvider.GetRequiredService<IdentityServerConfigurationDbContext>())
                {
                    await context.Database.MigrateAsync();
                }

                using (var context = scope.ServiceProvider.GetRequiredService<LogDbContext>())
                {
                    await context.Database.MigrateAsync();
                }

                using (var context = scope.ServiceProvider.GetRequiredService<AuditLogDbContext>())
                {
                    await context.Database.MigrateAsync();
                }

                using (var context = scope.ServiceProvider.GetRequiredService<DataProtectionDbContext>())
                {
                    await context.Database.MigrateAsync();
                }
            }
        }

        static ServiceProvider BuildServiceProvider(string[] args)
        {
            var services = new ServiceCollection();
            var configuration = BuildConfiguration(args);
            services.AddScoped<IConfiguration>(_ => configuration);
            
            var identityServerData = new IdentityServerData();
            configuration.Bind("IdentityServerData", identityServerData);      //  <--- This
            services.AddSingleton(identityServerData);
            
            var identityData = new IdentityData();
            configuration.Bind("IdentityData", identityData);      //  <--- This
            services.AddSingleton(identityData);
            
            services.AddDbContext<LogDbContext>(options =>
            {
               options.UseSqlServer(configuration.GetConnectionString(ConnectionStrings.LogDb));
            });

            services.AddDbContext<AuditLogDbContext>(options =>
            {
               options.UseSqlServer(configuration.GetConnectionString(ConnectionStrings.AuditLogDb));
            });

            services.AddDbContext<DataProtectionDbContext>(options =>
            {
               options.UseSqlServer(configuration.GetConnectionString(ConnectionStrings.DataProtectionDb));
            });

            services.AddDbContext<UserIdentityDbContext>(options =>
            {
               options.UseSqlServer(configuration.GetConnectionString(ConnectionStrings.UserIdentityDb));
            });

            services.AddIdentity<UserIdentity, UserIdentityRole>()
            .AddEntityFrameworkStores<UserIdentityDbContext>()
            .AddDefaultTokenProviders();

            var migrationsAssembly = typeof(UserIdentityDbContext).GetTypeInfo().Assembly.GetName().Name;

            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddAspNetIdentity<UserIdentity>()
                .AddConfigurationStore<IdentityServerConfigurationDbContext>(options =>
                {
                    options.ConfigureDbContext = 
                    builder => builder.UseSqlServer(configuration.GetConnectionString(ConnectionStrings.IdentityServerConfigurationDb),
                     sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                .AddOperationalStore<IdentityServerPersistedGrantDbContext>(options =>
                {
                    options.ConfigureDbContext = 
                    builder => builder.UseSqlServer(configuration.GetConnectionString(ConnectionStrings.IdentityServerPersistedGrantDb),
                     sql => sql.MigrationsAssembly(migrationsAssembly));
                    // this enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                    options.TokenCleanupInterval = 30;
                });

            return services.BuildServiceProvider();
        }

        private static IConfiguration BuildConfiguration(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("sharedsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"sharedsettings.{environment}.json", optional: true, reloadOnChange: true);

            return configurationBuilder.Build();
        }
    }
}
