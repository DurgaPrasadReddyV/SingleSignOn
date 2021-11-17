/* This class is needed for EF Core console commands
     * (like Add-Migration and Update-Database commands)
     * */
using System.IO;
using IdentityServer.Admin.EntityFramework.Shared.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class AdminAuditLogDbContextFactory : IDesignTimeDbContextFactory<AdminAuditLogDbContext>
    {
        public AdminAuditLogDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AdminAuditLogDbContext>()
                .UseSqlServer(GetConnectionStringFromConfiguration(), b =>
                {
                    b.MigrationsHistoryTable("__AdminAuditLog_Migrations");
                });

            return new AdminAuditLogDbContext(builder.Options);
        }

        private static string GetConnectionStringFromConfiguration()
        {
            return BuildConfiguration()
                .GetConnectionString("AdminAuditLogDbConnection");
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