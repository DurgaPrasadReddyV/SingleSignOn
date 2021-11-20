using System.IO;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SingleSignOn.EntityFrameworkCore.DbContextFactories
{
    public class IdentityServerDataProtectionDbContextFactory : IDesignTimeDbContextFactory<IdentityServerDataProtectionDbContext>
    {
        public IdentityServerDataProtectionDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<IdentityServerDataProtectionDbContext>()
                .UseSqlServer(GetConnectionStringFromConfiguration(), b =>
                {
                    b.MigrationsHistoryTable("__DataProtection_Migrations");
                });

            return new IdentityServerDataProtectionDbContext(builder.Options);
        }

        private static string GetConnectionStringFromConfiguration()
        {
            return BuildConfiguration()
                .GetConnectionString("DataProtectionDbConnection");
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
}