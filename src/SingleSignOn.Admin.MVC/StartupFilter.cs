using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Skoruba.IdentityServer4.Admin.UI.Configuration;
using Skoruba.IdentityServer4.Admin.UI.Helpers;

internal class StartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            {
                return builder =>
                {
                    // Adds our required middlewares to the beginning of the app pipeline.
                    // This does not include the middleware that is required to go between UseRouting and UseEndpoints.
                    builder.UseCommonMiddleware(
                        builder.ApplicationServices.GetRequiredService<SecurityConfiguration>(),
                        builder.ApplicationServices.GetRequiredService<HttpConfiguration>());

                    next(builder);

                    // Routing-dependent middleware needs to go in between UseRouting and UseEndpoints and therefore 
                    // needs to be handled by the user using UseIdentityServer4AdminUI().
                };
            }
        }