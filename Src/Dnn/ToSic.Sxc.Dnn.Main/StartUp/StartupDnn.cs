﻿using System.Configuration;
using System.Web.Hosting;
using DotNetNuke.Web.Api;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using ToSic.Eav;
using ToSic.Eav.Configuration;
using ToSic.SexyContent.Dnn920;
using ToSic.Sxc.Polymorphism;
using ToSic.Sxc.WebApi;
using GlobalConfiguration = System.Web.Http.GlobalConfiguration;

namespace ToSic.Sxc.Dnn.StartUp
{
    /// <summary>
    /// This configures .net Core Dependency Injection
    /// The StartUp is defined as an IServiceRouteMapper.
    /// This way DNN will auto-run this code before anything else
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class StartupDnn : IServiceRouteMapper
    {
        /// <summary>
        /// This will be called by DNN when loading the assemblies.
        /// We just want to trigger the DI-Configure
        /// </summary>
        /// <param name="mapRouteManager"></param>
        public void RegisterRoutes(IMapRoute mapRouteManager) => Configure();


        private static bool _alreadyConfigured;

        /// <summary>
        /// Configure IoC for 2sxc. If it's already configured, do nothing.
        /// </summary>
        public void Configure()
        {
            if (_alreadyConfigured)
                return;

            var appsCache = GetAppsCacheOverride();
            Eav.Factory.ActivateNetCoreDi(services =>
            {
                services
                    .AddDnn(appsCache)
                    .AddAdamWebApi<int, int>()
                    .AddSxcWebApi()
                    .AddSxcCore()
                    .AddEav();
                
                // temp polymorphism - later put into AddPolymorphism
                services.TryAddTransient<Koi>();
                services.TryAddTransient<Permissions>();
                
            });

            // Configure Newtonsoft Time zone handling
            // Moved here in v12.05 - previously it was in the Pre-Serialization converter
            GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;


            // now we should be able to instantiate registration of DB
            Eav.Factory.StaticBuild<IDbConfiguration>().ConnectionString = ConfigurationManager.ConnectionStrings["SiteSqlServer"].ConnectionString;
            var globalConfig = Eav.Factory.StaticBuild<IGlobalConfiguration>();

            globalConfig.GlobalFolder = HostingEnvironment.MapPath(DnnConstants.SysFolderRootVirtual);
            globalConfig.GlobalSiteFolder = "~/Portals/_default/";

            // Load features from configuration
            Eav.Factory.StaticBuild<SystemLoader>().StartUp();

            // also register this because of a long DNN issue which was fixed, but we don't know if we're running in another version
            SharpZipLibRedirect.RegisterSharpZipLibRedirect();

            // Help RazorBlade to have a proper best-practices ToJson
            // New v12.05
            Razor.Internals.StartUp.RegisterToJson(JsonConvert.SerializeObject);

            _alreadyConfigured = true;
        }


        /// <summary>
        /// Expects something like "ToSic.Sxc.Dnn.DnnAppsCacheFarm, ToSic.Sxc.Dnn.Enterprise" - namespaces + class, DLL name without extension
        /// </summary>
        /// <returns></returns>
        private string GetAppsCacheOverride()
        {
            var farmCacheName = ConfigurationManager.AppSettings["EavAppsCache"];
            if (string.IsNullOrWhiteSpace(farmCacheName)) return null;
            return farmCacheName;
        }
    }
}