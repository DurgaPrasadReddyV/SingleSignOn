using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using SingleSignOn.EntityFrameworkCore.Entities.Identity;
using SingleSignOn.AuthServer.MVC.Configuration;
using SingleSignOn.AuthServer.MVC.Configuration.Constants;
using SingleSignOn.AuthServer.MVC.Configuration.Interfaces;
using SingleSignOn.AuthServer.MVC.Helpers;
using System;
using Skoruba.IdentityServer4.Shared.Configuration.Helpers;

namespace SingleSignOn.AuthServer.MVC
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment, IConfiguration configuration)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var rootConfiguration = CreateRootConfiguration();
            
            services.AddSingleton(rootConfiguration);

            services.RegisterDbContexts<IdentityDbContext, 
                IdentityServerConfigurationDbContext, 
                IdentityServerPersistedGrantDbContext, 
                DataProtectionDbContext>(Configuration);

            services.AddDataProtection<DataProtectionDbContext>(Configuration);

            services.AddEmailSenders(Configuration);

            services.AddAuthenticationServices<IdentityDbContext, 
                UserIdentity, UserIdentityRole>(Configuration);

            services.AddIdentityServer<IdentityServerConfigurationDbContext,
                IdentityServerPersistedGrantDbContext, UserIdentity>(Configuration);

            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddMvcWithLocalization<UserIdentity, string>(Configuration);

            services.AddAuthorizationPolicies(rootConfiguration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCookiePolicy();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UsePathBase(Configuration.GetValue<string>("BasePath"));

            // Add custom security headers
            app.UseSecurityHeaders(Configuration);

            app.UseStaticFiles();
            app.UseIdentityServer();
            app.UseMvcLocalizationServices();

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoint =>
            {
                endpoint.MapDefaultControllerRoute();
                endpoint.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
            });
        }

        protected IRootConfiguration CreateRootConfiguration()
        {
            var rootConfiguration = new RootConfiguration();
            Configuration.GetSection(ConfigurationConsts.AdminConfigurationKey).Bind(rootConfiguration.AdminConfiguration);
            Configuration.GetSection(ConfigurationConsts.RegisterConfigurationKey).Bind(rootConfiguration.RegisterConfiguration);
            return rootConfiguration;
        }
    }
}








