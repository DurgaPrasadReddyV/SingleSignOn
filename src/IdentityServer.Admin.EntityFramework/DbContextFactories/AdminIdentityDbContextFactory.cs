/* This class is needed for EF Core console commands
     * (like Add-Migration and Update-Database commands)
     * */
using System.IO;
using IdentityServer.Admin.EntityFramework.Shared.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

public class AdminIdentityDbContextFactory : IDesignTimeDbContextFactory<AdminIdentityDbContext>
    {
        public AdminIdentityDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AdminIdentityDbContext>()
                .UseSqlServer(GetConnectionStringFromConfiguration(), b =>
                {
                    b.MigrationsHistoryTable("__Identity_Migrations");
                });

            return new AdminIdentityDbContext(builder.Options);
        }

        private static string GetConnectionStringFromConfiguration()
        {
            return BuildConfiguration()
                .GetConnectionString("IdentityDbConnection");
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