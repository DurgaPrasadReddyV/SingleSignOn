/* This class is needed for EF Core console commands
     * (like Add-Migration and Update-Database commands)
     * */
using System.IO;
using IdentityServer.Admin.EntityFramework.Shared.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class AdminLogDbContextFactory : IDesignTimeDbContextFactory<AdminLogDbContext>
    {
        public AdminLogDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AdminLogDbContext>()
                .UseSqlServer(GetConnectionStringFromConfiguration(), b =>
                {
                    b.MigrationsHistoryTable("__AdminLog_Migrations");
                });

            return new AdminLogDbContext(builder.Options);
        }

        private static string GetConnectionStringFromConfiguration()
        {
            return BuildConfiguration()
                .GetConnectionString("AdminLogDbConnection");
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