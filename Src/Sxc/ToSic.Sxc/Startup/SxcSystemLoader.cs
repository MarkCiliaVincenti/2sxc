﻿using ToSic.Eav.Configuration;

namespace ToSic.Sxc.Startup
{
    public class SxcSystemLoader
    {

        public SxcSystemLoader(EavSystemLoader eavLoader, FeaturesCatalog featuresCatalog)
        {
            _featuresCatalog = featuresCatalog;
            EavLoader = eavLoader;
        }
        private readonly FeaturesCatalog _featuresCatalog;
        public readonly EavSystemLoader EavLoader;

        public void StartUp()
        {
            PreStartUp();
            EavLoader.StartUp();
        }

        public void PreStartUp()
        {
            // Register Sxc features before loading
            Configuration.Features.BuiltInFeatures.Register(_featuresCatalog);
        }
    }
}