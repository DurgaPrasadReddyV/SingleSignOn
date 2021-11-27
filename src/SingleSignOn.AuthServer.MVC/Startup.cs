using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using SingleSignOn.EntityFrameworkCore.Entities;
using SingleSignOn.AuthServer.MVC.Configuration;
using SingleSignOn.AuthServer.MVC.Configuration.Constants;
using SingleSignOn.AuthServer.MVC.Configuration.Interfaces;
using SingleSignOn.AuthServer.MVC.Helpers;
using System;
using Skoruba.IdentityServer4.Shared.Configuration.Helpers;
using Skoruba.IdentityServer4.Shared.Configuration.Configuration.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Skoruba.IdentityServer4.Shared.Configuration.Authentication;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using SingleSignOn.AuthServer.MVC.Helpers.Localization;
using SingleSignOn.AuthServer.MVC.Configuration.ApplicationParts;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using SingleSignOn.EntityFrameworkCore.Constants;
using System.Reflection;
using IdentityServer4.EntityFramework.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Skoruba.IdentityServer4.Shared.Configuration.Email;
using Microsoft.AspNetCore.Identity.UI.Services;

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

            var migrationsAssembly = typeof(UserIdentityDbContext).GetTypeInfo().Assembly.GetName().Name;

            // Config DB for identity
            services.AddDbContext<UserIdentityDbContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString(ConnectionStrings.UserIdentityDb),
             sql => sql.MigrationsAssembly(migrationsAssembly)));

            // Config DB from existing connection
            services.AddConfigurationDbContext<IdentityServerConfigurationDbContext>(
                options => options.ConfigureDbContext = b => b.UseSqlServer(Configuration.GetConnectionString(ConnectionStrings.IdentityServerConfigurationDb),
              sql => sql.MigrationsAssembly(migrationsAssembly)));

            // Operational DB from existing connection
            services.AddOperationalDbContext<IdentityServerPersistedGrantDbContext>(
                options => options.ConfigureDbContext = b => b.UseSqlServer(Configuration.GetConnectionString(ConnectionStrings.IdentityServerPersistedGrantDb),
              sql => sql.MigrationsAssembly(migrationsAssembly)));

            // DataProtectionKey DB from existing connection
            services.AddDbContext<DataProtectionDbContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString(ConnectionStrings.DataProtectionDb),
             sql => sql.MigrationsAssembly(migrationsAssembly)));

            services.AddDataProtection()
                .SetApplicationName("Skoruba.IdentityServer4").PersistKeysToDbContext<DataProtectionDbContext>();

            services.AddSingleton<IEmailSender, LogEmailSender>();

            var loginConfiguration = Configuration.GetSection(nameof(LoginConfiguration)).Get<LoginConfiguration>();

            // Cannot load configuration - use default configuration values
            if (loginConfiguration == null)
            {
                loginConfiguration =  new LoginConfiguration();
            }
            services.AddSingleton(loginConfiguration);

            var registrationConfiguration = Configuration.GetSection(nameof(RegisterConfiguration)).Get<RegisterConfiguration>();

            // Cannot load configuration - use default configuration values
            if (registrationConfiguration == null)
            {
                registrationConfiguration =  new RegisterConfiguration();
            }
            services.AddSingleton(registrationConfiguration);

            var identityOptions = Configuration.GetSection(nameof(IdentityOptions)).Get<IdentityOptions>();
            services.AddSingleton(identityOptions);

            services.AddScoped<ApplicationSignInManager<UserIdentity>>()
                .AddScoped<UserResolver<UserIdentity>>()
                .AddIdentity<UserIdentity, UserIdentityRole>(options => Configuration.GetSection(nameof(IdentityOptions)).Bind(options))
                .AddEntityFrameworkStores<UserIdentityDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.Secure = CookieSecurePolicy.SameAsRequest;
                options.OnAppendCookie = cookieContext => AuthenticationHelpers.CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext => AuthenticationHelpers.CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });

            services.Configure<IISOptions>(iis =>
            {
                iis.AuthenticationDisplayName = "Windows";
                iis.AutomaticAuthentication = false;
            });

            services.AddAuthentication();

            var advancedConfiguration = Configuration.GetSection(nameof(AdvancedConfiguration)).Get<AdvancedConfiguration>();

            var builder = services.AddIdentityServer(options =>
                {
                    options.Events.RaiseErrorEvents = true;
                    options.Events.RaiseInformationEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseSuccessEvents = true;
                    
                    if (!string.IsNullOrEmpty(advancedConfiguration.IssuerUri))
                    {
                        options.IssuerUri = advancedConfiguration.IssuerUri;
                    }
                })
                .AddConfigurationStore<IdentityServerConfigurationDbContext>()
                .AddOperationalStore<IdentityServerPersistedGrantDbContext>()
                .AddAspNetIdentity<UserIdentity>()
                .AddDeveloperSigningCredential();

            builder.AddExtensionGrantValidator<DelegationGrantValidator>();

            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.AddLocalization(opts => { opts.ResourcesPath = ConfigurationConsts.ResourcesPath; });

            services.TryAddTransient(typeof(IGenericControllerLocalizer<>), typeof(GenericControllerLocalizer<>));

            services.AddControllersWithViews(o =>
                {
                    o.Conventions.Add(new GenericControllerRouteConvention());
                })
                .AddViewLocalization(
                    LanguageViewLocationExpanderFormat.Suffix,
                    opts => { opts.ResourcesPath = ConfigurationConsts.ResourcesPath; })
                .AddDataAnnotationsLocalization()
                .ConfigureApplicationPartManager(m =>
                {
                    m.FeatureProviders.Add(new GenericTypeControllerFeatureProvider<UserIdentity, string>());
                });

            var cultureConfiguration = Configuration.GetSection(nameof(CultureConfiguration)).Get<CultureConfiguration>();
            services.Configure<RequestLocalizationOptions>(
                opts =>
                {
                    // If cultures are specified in the configuration,
                    // use them (making sure they are among the available cultures),
                    // otherwise use all the available cultures
                    var supportedCultureCodes = (cultureConfiguration?.Cultures?.Count > 0 ?
                        cultureConfiguration.Cultures.Intersect(CultureConfiguration.AvailableCultures) :
                        CultureConfiguration.AvailableCultures).ToArray();

                    if (!supportedCultureCodes.Any()) supportedCultureCodes = CultureConfiguration.AvailableCultures;
                    var supportedCultures = supportedCultureCodes.Select(c => new CultureInfo(c)).ToList();

                    // If the default culture is specified use it,
                    // otherwise use CultureConfiguration.DefaultRequestCulture ("en")
                    var defaultCultureCode = string.IsNullOrEmpty(cultureConfiguration?.DefaultCulture) ?
                        CultureConfiguration.DefaultRequestCulture : cultureConfiguration?.DefaultCulture;

                    // If the default culture is not among the supported cultures, use the first supported culture as default
                    if (!supportedCultureCodes.Contains(defaultCultureCode)) defaultCultureCode = 
                        supportedCultureCodes.FirstOrDefault();

                    opts.DefaultRequestCulture = new RequestCulture(defaultCultureCode);
                    opts.SupportedCultures = supportedCultures;
                    opts.SupportedUICultures = supportedCultures;
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationConsts.AdministrationPolicy,
                    policy => policy.RequireRole(rootConfiguration.AdminConfiguration.AdministrationRole));
            });
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

            var forwardingOptions = new ForwardedHeadersOptions()
            {
                ForwardedHeaders = ForwardedHeaders.All
            };

            forwardingOptions.KnownNetworks.Clear();
            forwardingOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(forwardingOptions);

            app.UseReferrerPolicy(options => options.NoReferrer());

            // CSP Configuration to be able to use external resources
            var cspTrustedDomains = new List<string>();
            Configuration.GetSection(ConfigurationConsts.CspTrustedDomainsKey).Bind(cspTrustedDomains);
            if (cspTrustedDomains.Any())
            {
                app.UseCsp(csp =>
                {
                    csp.ImageSources(options =>
                    {
                        options.SelfSrc = true;
                        options.CustomSources = cspTrustedDomains;
                        options.Enabled = true;
                    });
                    csp.FontSources(options =>
                    {
                        options.SelfSrc = true;
                        options.CustomSources = cspTrustedDomains;
                        options.Enabled = true;
                    });
                    csp.ScriptSources(options =>
                    {
                        options.SelfSrc = true;
                        options.CustomSources = cspTrustedDomains;
                        options.Enabled = true;
                        options.UnsafeInlineSrc = true;
                    });
                    csp.StyleSources(options =>
                    {
                        options.SelfSrc = true;
                        options.CustomSources = cspTrustedDomains;
                        options.Enabled = true;
                        options.UnsafeInlineSrc = true;
                    });
                    csp.DefaultSources(options =>
                    {
                        options.SelfSrc = true;
                        options.CustomSources = cspTrustedDomains;
                        options.Enabled = true;
                    });
                });
            }

            app.UseStaticFiles();
            app.UseIdentityServer();
            
            app.UseRequestLocalization(app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>().Value);

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoint =>
            {
                endpoint.MapDefaultControllerRoute();
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








