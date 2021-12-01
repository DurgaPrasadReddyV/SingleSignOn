using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Skoruba.AuditLogging.EntityFramework.Entities;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using SingleSignOn.EntityFrameworkCore.Entities;
using Skoruba.IdentityServer4.Shared.Configuration.Helpers;
using SingleSignOn.Admin.MVC.Dtos;
using SingleSignOn.Admin.MVC.Dtos.Identity;
using Skoruba.IdentityServer4.Shared.Configuration.Email;
using Microsoft.AspNetCore.Identity.UI.Services;
using Skoruba.IdentityServer4.Admin.UI.Helpers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System;
using IdentityServer4.EntityFramework.Storage;
using Skoruba.IdentityServer4.Admin.UI.Configuration.Constants;
using Skoruba.IdentityServer4.Admin.EntityFramework.Repositories.Interfaces;
using Skoruba.IdentityServer4.Admin.EntityFramework.Repositories;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Services.Interfaces;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Services;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Resources;
using Skoruba.IdentityServer4.Admin.EntityFramework.Identity.Repositories.Interfaces;
using Skoruba.IdentityServer4.Admin.EntityFramework.Identity.Repositories;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Identity.Resources;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Identity.Services.Interfaces;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Identity.Mappers.Configuration;
using AutoMapper;
using Skoruba.IdentityServer4.Admin.BusinessLogic.Identity.Services;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Skoruba.IdentityServer4.Admin.UI.Helpers.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using Skoruba.IdentityServer4.Admin.UI.Configuration.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Skoruba.IdentityServer4.Admin.UI.Configuration;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Linq;
using SingleSignOn.EntityFrameworkCore.Constants;
using System.Globalization;

namespace IdentityServer.Admin
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            HostingEnvironment = env;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment HostingEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var options = new IdentityServer4AdminUIOptions();
            options.BindConfiguration(Configuration);
            
            if (HostingEnvironment.IsDevelopment())
            {
                options.Security.UseDeveloperExceptionPage = true;
            }
            else
            {
                options.Security.UseHsts = true;
            }

            // Adds root configuration to the DI.
            services.AddSingleton(options.Admin);
            services.AddSingleton(options.IdentityServerData);
            services.AddSingleton(options.IdentityData);

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

             // Log DB from existing connection
            services.AddDbContext<LogDbContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString(ConnectionStrings.LogDb),
                sql => sql.MigrationsAssembly(migrationsAssembly)));

            // Audit logging connection
            services.AddDbContext<AuditLogDbContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString(ConnectionStrings.LogDb),
                sql => sql.MigrationsAssembly(migrationsAssembly)));

            services.AddDataProtection()
                .SetApplicationName("Skoruba.IdentityServer4").PersistKeysToDbContext<DataProtectionDbContext>();


                services.AddAuthenticationServices<UserIdentityDbContext, UserIdentity, UserIdentityRole>
                        (options.Admin, options.IdentityConfigureAction, options.Security.AuthenticationBuilderAction);


            // Add HSTS options
            if (options.Security.UseHsts)
            {
                services.AddHsts(opt =>
                {
                    opt.Preload = true;
                    opt.IncludeSubDomains = true;
                    opt.MaxAge = TimeSpan.FromDays(365);

                    options.Security.HstsConfigureAction?.Invoke(opt);
                });
            }

            // Add exception filters in MVC
            services.AddMvcExceptionFilters();

            //Repositories
            services.AddTransient<IClientRepository, ClientRepository<IdentityServerConfigurationDbContext>>();
            services.AddTransient<IIdentityResourceRepository, IdentityResourceRepository<IdentityServerConfigurationDbContext>>();
            services.AddTransient<IApiResourceRepository, ApiResourceRepository<IdentityServerConfigurationDbContext>>();
            services.AddTransient<IApiScopeRepository, ApiScopeRepository<IdentityServerConfigurationDbContext>>();
            services.AddTransient<IPersistedGrantRepository, PersistedGrantRepository<IdentityServerPersistedGrantDbContext>>();
            services.AddTransient<ILogRepository, LogRepository<LogDbContext>>();

            //Services
            services.AddTransient<IClientService, ClientService>();
            services.AddTransient<IApiResourceService, ApiResourceService>();
            services.AddTransient<IApiScopeService, ApiScopeService>();
            services.AddTransient<IIdentityResourceService, IdentityResourceService>();
            services.AddTransient<IPersistedGrantService, PersistedGrantService>();
            services.AddTransient<ILogService, LogService>();

            //Resources
            services.AddScoped<IApiResourceServiceResources, ApiResourceServiceResources>();
            services.AddScoped<IApiScopeServiceResources, ApiScopeServiceResources>();
            services.AddScoped<IClientServiceResources, ClientServiceResources>();
            services.AddScoped<IIdentityResourceServiceResources, IdentityResourceServiceResources>();
            services.AddScoped<IPersistedGrantServiceResources, PersistedGrantServiceResources>();

            //Repositories
            services.AddTransient<IIdentityRepository<UserIdentity, UserIdentityRole, string, UserIdentityUserClaim,
             UserIdentityUserRole, UserIdentityUserLogin, UserIdentityRoleClaim, UserIdentityUserToken>,
              IdentityRepository<UserIdentityDbContext, UserIdentity, UserIdentityRole, string, UserIdentityUserClaim, UserIdentityUserRole, UserIdentityUserLogin, UserIdentityRoleClaim, UserIdentityUserToken>>();
            services.AddTransient<IPersistedGrantAspNetIdentityRepository, PersistedGrantAspNetIdentityRepository<UserIdentityDbContext, IdentityServerPersistedGrantDbContext
            , UserIdentity, UserIdentityRole, string, UserIdentityUserClaim, UserIdentityUserRole, UserIdentityUserLogin, UserIdentityRoleClaim, UserIdentityUserToken>>();
          
            //Services
            services.AddTransient<IIdentityService<IdentityUserDto, IdentityRoleDto, UserIdentity, UserIdentityRole, string, UserIdentityUserClaim,
             UserIdentityUserRole, UserIdentityUserLogin, UserIdentityRoleClaim, UserIdentityUserToken,
                IdentityUsersDto, IdentityRolesDto, IdentityUserRolesDto, IdentityUserClaimsDto,
                IdentityUserProviderDto, IdentityUserProvidersDto, IdentityUserChangePasswordDto,
                 IdentityRoleClaimsDto, IdentityUserClaimDto, IdentityRoleClaimDto>, 
                IdentityService<IdentityUserDto, IdentityRoleDto, UserIdentity, UserIdentityRole, string, UserIdentityUserClaim,
             UserIdentityUserRole, UserIdentityUserLogin, UserIdentityRoleClaim, UserIdentityUserToken,
                IdentityUsersDto, IdentityRolesDto, IdentityUserRolesDto, IdentityUserClaimsDto,
                IdentityUserProviderDto, IdentityUserProvidersDto, IdentityUserChangePasswordDto,
                 IdentityRoleClaimsDto, IdentityUserClaimDto, IdentityRoleClaimDto>>();
            
            services.AddTransient<IPersistedGrantAspNetIdentityService, PersistedGrantAspNetIdentityService>();
            
            //Resources
            services.AddScoped<IIdentityServiceResources, IdentityServiceResources>();
            services.AddScoped<IPersistedGrantAspNetIdentityServiceResources, PersistedGrantAspNetIdentityServiceResources>();

            //Register mapping
            var builder = new MapperConfigurationBuilder();

            services.AddSingleton<AutoMapper.IConfigurationProvider>(sp => new MapperConfiguration(cfg =>
            {
                foreach (var profileType in builder.ProfileTypes)
                    cfg.AddProfile(profileType);
            }));

            services.AddScoped<IMapper>(sp => new Mapper(sp.GetRequiredService<AutoMapper.IConfigurationProvider>(), sp.GetService));

            builder.UseIdentityMappingProfile<IdentityUserDto, IdentityRoleDto, UserIdentity, UserIdentityRole, string, UserIdentityUserClaim,
             UserIdentityUserRole, UserIdentityUserLogin, UserIdentityRoleClaim, UserIdentityUserToken,
                IdentityUsersDto, IdentityRolesDto, IdentityUserRolesDto, IdentityUserClaimsDto,
                IdentityUserProviderDto, IdentityUserProvidersDto, IdentityUserChangePasswordDto,
                 IdentityRoleClaimsDto, IdentityUserClaimDto, IdentityRoleClaimDto>()
                .AddProfilesType(null);

            // Add all dependencies for Asp.Net Core Identity in MVC - these dependencies are injected into generic Controllers
            // Including settings for MVC and Localization
            services.AddSingleton<ITempDataProvider, CookieTempDataProvider>();

            services.AddLocalization(opts => { opts.ResourcesPath = ConfigurationConsts.ResourcesPath; });

            services.TryAddTransient(typeof(IGenericControllerLocalizer<>), typeof(GenericControllerLocalizer<>));

            services.AddTransient<IViewLocalizer, ResourceViewLocalizer>();

            services.AddControllersWithViews(o =>
                {
                    o.Conventions.Add(new GenericControllerRouteConvention());
                })
                .AddRazorRuntimeCompilation()
                .AddViewLocalization(
                    LanguageViewLocationExpanderFormat.Suffix,
                    opts => { opts.ResourcesPath = ConfigurationConsts.ResourcesPath; })
                .AddDataAnnotationsLocalization()
                .ConfigureApplicationPartManager(m =>
                {
                    m.FeatureProviders.Add(new GenericTypeControllerFeatureProvider<IdentityUserDto, IdentityRoleDto, UserIdentity, UserIdentityRole, string, UserIdentityUserClaim,
             UserIdentityUserRole, UserIdentityUserLogin, UserIdentityRoleClaim, UserIdentityUserToken,
                IdentityUsersDto, IdentityRolesDto, IdentityUserRolesDto, IdentityUserClaimsDto,
                IdentityUserProviderDto, IdentityUserProvidersDto, IdentityUserChangePasswordDto,
                 IdentityRoleClaimsDto, IdentityUserClaimDto, IdentityRoleClaimDto>());
                });

            services.Configure<RequestLocalizationOptions>(
                opts =>
                {
                     // If cultures are specified in the configuration, use them (making sure they are among the available cultures),
                    // otherwise use all the available cultures
                    var supportedCultureCodes = (options.Culture?.Cultures?.Count > 0 ?
                        options.Culture.Cultures.Intersect(CultureConfiguration.AvailableCultures) :
                        CultureConfiguration.AvailableCultures).ToArray();

                    if (!supportedCultureCodes.Any()) supportedCultureCodes = CultureConfiguration.AvailableCultures;
                    var supportedCultures = supportedCultureCodes.Select(c => new CultureInfo(c)).ToList();

                    // If the default culture is specified use it, otherwise use CultureConfiguration.DefaultRequestCulture ("en")
                    var defaultCultureCode = string.IsNullOrEmpty(options.Culture?.DefaultCulture) ?
                        CultureConfiguration.DefaultRequestCulture : options.Culture?.DefaultCulture;

                    // If the default culture is not among the supported cultures, use the first supported culture as default
                    if (!supportedCultureCodes.Contains(defaultCultureCode)) defaultCultureCode = supportedCultureCodes.FirstOrDefault();

                    opts.DefaultRequestCulture = new RequestCulture(defaultCultureCode);
                    opts.SupportedCultures = supportedCultures;
                    opts.SupportedUICultures = supportedCultures;
                });

            services.AddAuthorization(loptions =>
            {
                loptions.AddPolicy(AuthorizationConsts.AdministrationPolicy,
                    policy => policy.RequireRole(options.Admin.AdministrationRole));

                options.Security.AuthorizationConfigureAction?.Invoke(loptions);
            });

            // Add audit logging
            services.AddAuditEventLogging<AuditLogDbContext, AuditLog>(options.AuditLogging);

            // Adds a startup filter for further middleware configuration.
            services.AddSingleton(options.Testing);
            services.AddSingleton(options.Security);
            services.AddSingleton(options.Http);
            services.AddTransient<IStartupFilter, StartupFilter>();

            services.AddSingleton<IEmailSender, LogEmailSender>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseRouting();

            app.UseIdentityServer4AdminUI();

            app.UseEndpoints(endpoint =>
            {
                endpoint.MapIdentityServer4AdminUI();
            });
        }
    }
}







