using System.IO;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SingleSignOn.EntityFrameworkCore.DbContextFactories
{
    public class AuditLogDbContextFactory : IDesignTimeDbContextFactory<AuditLogDbContext>
    {
        public AuditLogDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AuditLogDbContext>()
                .UseSqlServer(GetConnectionStringFromConfiguration(), b =>
                {
                    b.MigrationsHistoryTable("__AuditLog_Migrations");
                });

            return new AuditLogDbContext(builder.Options);
        }

        private static string GetConnectionStringFromConfiguration()
        {
            return BuildConfiguration()
                .GetConnectionString("AdminAuditLogDbConnection");
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(),$".."))
                .AddJsonFile("sharedsettings.json", optional: false);

            return builder.Build();
        }
    }
}