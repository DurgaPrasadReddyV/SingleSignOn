using System.IO;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SingleSignOn.EntityFrameworkCore.DbContextFactories
{
    public class DataProtectionDbContextFactory : IDesignTimeDbContextFactory<DataProtectionDbContext>
    {
        public DataProtectionDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<DataProtectionDbContext>()
                .UseSqlServer(GetConnectionStringFromConfiguration(), b =>
                {
                    b.MigrationsHistoryTable("__DataProtection_Migrations");
                });

            return new DataProtectionDbContext(builder.Options);
        }

        private static string GetConnectionStringFromConfiguration()
        {
            return BuildConfiguration()
                .GetConnectionString("DataProtectionDbConnection");
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