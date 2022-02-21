﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ToSic.Eav.Run;
using ToSic.Sxc.Context;
using ToSic.Sxc.Oqt.Server.Context;
using ToSic.Sxc.Oqt.Server.Installation;
using ToSic.Sxc.Oqt.Server.Integration;
using ToSic.Sxc.Oqt.Server.Plumbing;
using ToSic.Sxc.Oqt.Server.Run;
using ToSic.Sxc.Run;
using ToSic.Sxc.Web;

namespace ToSic.Sxc.Oqt.Server.StartUp
{
    internal static partial class OqtRegisterServices
    {
        /// <summary>
        /// Path resolvers and IPlatform information
        /// </summary>
        private static IServiceCollection AddSxcOqtPathsAndPlatform(this IServiceCollection services)
        {
            services.TryAddTransient<IServerPaths, OqtServerPaths>();
            services.AddScoped<ILinkPaths, OqtLinkPaths>();

            // TODO: Review - it looks a bit fishy to have the same class as singleton and transient
            services.AddSingleton<IPlatform, OqtPlatformContext>();
            services.TryAddTransient<IPlatformInfo, OqtPlatformContext>();
            return services;
        }
        
        /// <summary>
        /// Plumbing helpers to make sure everything can work, the context is ready etc.
        /// </summary>
        private static IServiceCollection AddOqtanePlumbing(this IServiceCollection services)
        {
            // Helper to access settings of a Site, Module etc.
            services.TryAddTransient<SettingsHelper>();

            // TODO: DOCUMENT what this does
            services.TryAddScoped<RequestHelper>();
            services.TryAddTransient<OqtCulture>();

            // Site State Initializer for APIs etc. to ensure that the SiteState exists and is correctly preloaded
            services.TryAddTransient<SiteStateInitializer>();

            // Views / Templates / Razor: Get url params in the request
            services.TryAddTransient<IHttp, HttpBlazor>();

            return services;
        }
        

        private static IServiceCollection AddOqtaneInstallation(this IServiceCollection services)
        {
            // Installation: Helper to ensure the installation is complete
            services.TryAddTransient<IEnvironmentInstaller, OqtEnvironmentInstaller>();

            // Installation: Verify the Razor Helper DLLs are available
            services.TryAddSingleton<GlobalTypesCheck>();

            return services;
        }
    }
}