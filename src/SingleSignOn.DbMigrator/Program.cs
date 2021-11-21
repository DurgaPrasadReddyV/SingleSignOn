using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Serilog;
using SingleSignOn.EntityFrameworkCore.Constants;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SingleSignOn.DbMigrator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Async(c => c.File("Logs/logs.txt"))
                .WriteTo.Async(c => c.Console())
                .CreateLogger();

            var configuration = GetConfiguration(args);

            using (var logDbContext = new LogDbContext(new DbContextOptionsBuilder<LogDbContext>()
                .UseSqlServer(configuration.GetConnectionString(ConnectionStrings.LogDb)).Options))
            {
                await logDbContext.Database.MigrateAsync();
            }

            using (var auditLogDbContext = new AuditLogDbContext(new DbContextOptionsBuilder<AuditLogDbContext>()
                .UseSqlServer(configuration.GetConnectionString(ConnectionStrings.AuditLogDb)).Options))
            {
                await auditLogDbContext.Database.MigrateAsync();
            }

            using (var dataProtectionDbContext = new DataProtectionDbContext(new DbContextOptionsBuilder<DataProtectionDbContext>()
                .UseSqlServer(configuration.GetConnectionString(ConnectionStrings.DataProtectionDb)).Options))
            {
                await dataProtectionDbContext.Database.MigrateAsync();
            }

            using (var identityServerConfigurationDbContext = new IdentityServerConfigurationDbContext(new DbContextOptionsBuilder<IdentityServerConfigurationDbContext>()
                .UseSqlServer(configuration.GetConnectionString(ConnectionStrings.IdentityServerConfigurationDb)).Options, new ConfigurationStoreOptions()))
            {
                await identityServerConfigurationDbContext.Database.MigrateAsync();
            }

            using (var identityServerPersistedGrantDbContext = new IdentityServerPersistedGrantDbContext(new DbContextOptionsBuilder<IdentityServerPersistedGrantDbContext>()
                .UseSqlServer(configuration.GetConnectionString(ConnectionStrings.IdentityServerPersistedGrantDb)).Options, new OperationalStoreOptions()))
            {
                await identityServerPersistedGrantDbContext.Database.MigrateAsync();
            }

            using (var userIdentityDbContext = new UserIdentityDbContext(new DbContextOptionsBuilder<UserIdentityDbContext>()
                .UseSqlServer(configuration.GetConnectionString(ConnectionStrings.UserIdentityDb)).Options))
            {
                await userIdentityDbContext.Database.MigrateAsync();
            }

        }

        private static IConfiguration GetConfiguration(string[] args)
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
