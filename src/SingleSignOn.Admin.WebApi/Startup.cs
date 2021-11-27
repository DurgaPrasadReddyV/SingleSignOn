using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Skoruba.AuditLogging.EntityFramework.Entities;
using SingleSignOn.Admin.WebApi.Configuration;
using SingleSignOn.Admin.WebApi.Configuration.Authorization;
using SingleSignOn.Admin.WebApi.ExceptionHandling;
using SingleSignOn.Admin.WebApi.Helpers;
using SingleSignOn.Admin.WebApi.Mappers;
using SingleSignOn.Admin.WebApi.Resources;
using SingleSignOn.EntityFrameworkCore.DbContexts;
using SingleSignOn.EntityFrameworkCore.Entities;
using Skoruba.IdentityServer4.Shared.Configuration.Helpers;
using SingleSignOn.Admin.WebApi.Dtos;
using SingleSignOn.Admin.WebApi.Dtos.Identity;
using SingleSignOn.Admin.WebApi.AuditLogging;
using Skoruba.AuditLogging.EntityFramework.Services;
using Skoruba.AuditLogging.EntityFramework.Extensions;
using Skoruba.AuditLogging.EntityFramework.Repositories;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SingleSignOn.Admin.WebApi.Helpers.Localization;
using SingleSignOn.Admin.WebApi.Configuration.ApplicationParts;
using SingleSignOn.Admin.WebApi.Configuration.Constants;
using Skoruba.IdentityServer4.Admin.EntityFramework.Configuration.Configuration;
using Skoruba.IdentityServer4.Admin.EntityFramework.Configuration.SqlServer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using IdentityServer4.EntityFramework.Storage;
using SingleSignOn.EntityFrameworkCore.Constants;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity.UI.Services;
using Skoruba.IdentityServer4.Shared.Configuration.Email;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace SingleSignOn.Admin.WebApi
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
            var adminApiConfiguration = Configuration.GetSection(nameof(AdminApiConfiguration)).Get<AdminApiConfiguration>();
            services.AddSingleton(adminApiConfiguration);

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

            services.AddSingleton<IEmailSender, LogEmailSender>();

            services.AddScoped<ControllerExceptionFilterAttribute>();
            services.AddScoped<IApiErrorResources, ApiErrorResources>();

            services.AddIdentity<UserIdentity, UserIdentityRole>(options => Configuration.GetSection(nameof(IdentityOptions)).Bind(options))
                .AddEntityFrameworkStores<UserIdentityDbContext>()
                .AddDefaultTokenProviders();

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = adminApiConfiguration.IdentityServerBaseUrl;
                    options.RequireHttpsMetadata = adminApiConfiguration.RequireHttpsMetadata;
                    options.Audience = adminApiConfiguration.OidcApiName;
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthorizationConsts.AdministrationPolicy,
                    policy =>
                        policy.RequireAssertion(context => context.User.HasClaim(c =>
                                ((c.Type == JwtClaimTypes.Role && c.Value == adminApiConfiguration.AdministrationRole) 
                                || (c.Type == $"client_{JwtClaimTypes.Role}" && c.Value == adminApiConfiguration.AdministrationRole))) 
                        && context.User.HasClaim(c => c.Type == JwtClaimTypes.Scope && c.Value == adminApiConfiguration.OidcApiName)
                        ));
            });

            var profileTypes = new HashSet<Type>
            {
                typeof(IdentityMapperProfile<IdentityRoleDto, IdentityUserRolesDto, string, IdentityUserClaimsDto, 
                IdentityUserClaimDto, IdentityUserProviderDto, IdentityUserProvidersDto, IdentityUserChangePasswordDto, 
                IdentityRoleClaimDto, IdentityRoleClaimsDto>)
            };

            services.AddAdminAspNetIdentityServices<UserIdentityDbContext, IdentityServerPersistedGrantDbContext,
                IdentityUserDto, IdentityRoleDto, UserIdentity, UserIdentityRole, string, 
                UserIdentityUserClaim, UserIdentityUserRole,
                UserIdentityUserLogin, UserIdentityRoleClaim, UserIdentityUserToken,
                IdentityUsersDto, IdentityRolesDto, IdentityUserRolesDto,
                IdentityUserClaimsDto, IdentityUserProviderDto, IdentityUserProvidersDto, IdentityUserChangePasswordDto,
                IdentityRoleClaimsDto, IdentityUserClaimDto, IdentityRoleClaimDto>(profileTypes);

            services.AddAdminServices<IdentityServerConfigurationDbContext, 
                IdentityServerPersistedGrantDbContext, LogDbContext>();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        if (adminApiConfiguration.CorsAllowAnyOrigin)
                        {
                            builder.AllowAnyOrigin();
                        }
                        else
                        {
                            builder.WithOrigins(adminApiConfiguration.CorsAllowOrigins);
                        }

                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                    });
            });

            services.AddLocalization(opts => { opts.ResourcesPath = ConfigurationConsts.ResourcesPath; });

            services.TryAddTransient(typeof(IGenericControllerLocalizer<>), typeof(GenericControllerLocalizer<>));

            services.AddControllersWithViews(o => { o.Conventions.Add(new GenericControllerRouteConvention()); })
                .AddDataAnnotationsLocalization()
                .ConfigureApplicationPartManager(m =>
                {
                    m.FeatureProviders.Add(new GenericTypeControllerFeatureProvider<IdentityUserDto, IdentityRoleDto,
                            UserIdentity, UserIdentityRole, string, UserIdentityUserClaim, UserIdentityUserRole,
                             UserIdentityUserLogin, UserIdentityRoleClaim, UserIdentityUserToken,
                            IdentityUsersDto, IdentityRolesDto, IdentityUserRolesDto, IdentityUserClaimsDto,
                            IdentityUserProviderDto, IdentityUserProvidersDto, IdentityUserChangePasswordDto,
                            IdentityRoleClaimsDto, IdentityUserClaimDto, IdentityRoleClaimDto>());
                });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc(adminApiConfiguration.ApiVersion, new OpenApiInfo 
                { Title = adminApiConfiguration.ApiName, Version = adminApiConfiguration.ApiVersion });

                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri($"{adminApiConfiguration.IdentityServerBaseUrl}/connect/authorize"),
                            TokenUrl = new Uri($"{adminApiConfiguration.IdentityServerBaseUrl}/connect/token"),
                            Scopes = new Dictionary<string, string> {
                                { adminApiConfiguration.OidcApiName, adminApiConfiguration.ApiName }
                            }
                        }
                    }
                });
                options.OperationFilter<AuthorizeCheckOperationFilter>();
            });

            var auditLoggingConfiguration = Configuration.GetSection(nameof(AuditLoggingConfiguration)).Get<AuditLoggingConfiguration>();
            services.AddSingleton(auditLoggingConfiguration);

            services.AddAuditLogging(options => { options.Source = auditLoggingConfiguration.Source; })
                .AddEventData<ApiAuditSubject, ApiAuditAction>()
                .AddAuditSinks<DatabaseAuditEventLoggerSink<AuditLog>>();

            services.AddTransient<IAuditLoggingRepository<AuditLog>, AuditLoggingRepository<AuditLogDbContext, AuditLog>>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AdminApiConfiguration adminApiConfiguration)
        {
            var forwardingOptions = new ForwardedHeadersOptions()
            {
                ForwardedHeaders = ForwardedHeaders.All
            };

            forwardingOptions.KnownNetworks.Clear();
            forwardingOptions.KnownProxies.Clear();

            app.UseForwardedHeaders(forwardingOptions);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"{adminApiConfiguration.ApiBaseUrl}/swagger/v1/swagger.json", adminApiConfiguration.ApiName);
                c.OAuthClientId(adminApiConfiguration.OidcSwaggerUIClientId);
                c.OAuthAppName(adminApiConfiguration.ApiName);
                c.OAuthUsePkce();
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseCors();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}








