using System.IO;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SingleSignOn.EntityFrameworkCore.DbContextFactories
{
    public class UserIdentityDbContextFactory : IDesignTimeDbContextFactory<UserIdentityDbContext>
    {
        public UserIdentityDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<UserIdentityDbContext>()
                .UseSqlServer(GetConnectionStringFromConfiguration(), b =>
                {
                    b.MigrationsHistoryTable("__Identity_Migrations");
                });

            return new UserIdentityDbContext(builder.Options);
        }

        private static string GetConnectionStringFromConfiguration()
        {
            return BuildConfiguration()
                .GetConnectionString("IdentityDbConnection");
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), $".."))
                .AddJsonFile("sharedsettings.json", optional: false);

            return builder.Build();
        }
    }
}