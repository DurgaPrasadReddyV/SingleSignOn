using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Skoruba.IdentityServer4.Shared.Configuration.Helpers;

namespace SingleSignOn.AuthServer.MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {

                CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                 .ConfigureAppConfiguration((hostContext, configApp) =>
                 {
                     var env = hostContext.HostingEnvironment;
                     var sourcePath = Path.Combine(env.ContentRootPath, "..");
                     configApp.AddJsonFile(Path.Combine(sourcePath, "sharedsettings.json"), optional: true, reloadOnChange: true);
                     configApp.AddJsonFile("sharedsettings.json", optional: true, reloadOnChange: true);
                     configApp.AddJsonFile($"sharedsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                 })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options => options.AddServerHeader = false);
                    webBuilder.UseStartup<Startup>();
                });
    }
}







