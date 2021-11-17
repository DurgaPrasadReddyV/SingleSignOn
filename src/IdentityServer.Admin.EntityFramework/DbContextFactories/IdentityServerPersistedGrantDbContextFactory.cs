/* This class is needed for EF Core console commands
     * (like Add-Migration and Update-Database commands)
     * */
using System.IO;
using IdentityServer.Admin.EntityFramework.Shared.DbContexts;
using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class IdentityServerPersistedGrantDbContextFactory : IDesignTimeDbContextFactory<IdentityServerPersistedGrantDbContext>
    {
        public IdentityServerPersistedGrantDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<IdentityServerPersistedGrantDbContext>()
                .UseSqlServer(GetConnectionStringFromConfiguration(), b =>
                {
                    b.MigrationsHistoryTable("__PersistedGrant_Migrations");
                });

            return new IdentityServerPersistedGrantDbContext(builder.Options,new OperationalStoreOptions());
        }

        private static string GetConnectionStringFromConfiguration()
        {
            return BuildConfiguration()
                .GetConnectionString("PersistedGrantDbConnection");
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        $"..{Path.DirectorySeparatorChar}IdentityServer.Admin.Api"
                    )
                )
                .AddJsonFile("appsettings.json", optional: false);

            return builder.Build();
        }
    }