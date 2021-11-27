using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Skoruba.IdentityServer4.Admin.EntityFramework.Configuration.Configuration;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using SingleSignOn.EntityFrameworkCore.Entities;
using SingleSignOn.EntityFrameworkCore.Helpers;
using Skoruba.IdentityServer4.Shared.Configuration.Helpers;

namespace IdentityServer.Admin
{
	public class Program
    {
        public static void Main(string[] args)
        {

                var host = CreateHostBuilder(args).Build();

                host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                 .ConfigureAppConfiguration((hostContext, configApp) =>
                 {
                     var env = hostContext.HostingEnvironment;
                     configApp.AddJsonFile($"sharedsettings.json", optional: false, reloadOnChange: true);
                     configApp.AddJsonFile($"sharedsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                 })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options => options.AddServerHeader = false);
                    webBuilder.UseStartup<Startup>();
                });
    }
}







