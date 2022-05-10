﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using ToSic.Eav.Apps;
using ToSic.Eav.Documentation;

namespace ToSic.Sxc.Web.LightSpeed
{
    [PrivateApi]
    public class OutputCacheManager
    {
        internal const string GlobalCacheRoot = "2sxc.Lightspeed.Module.";

        internal string Id(int moduleId, int? userId, string view, string suffix)
        {
            var id = GlobalCacheRoot + moduleId;
            if (userId.HasValue) id += ":" + userId.Value;
            if (view != null) id += ":" + view;
            if (suffix != null) id += ":" + suffix;
            return id;
        }

        public string Add(string cacheKey, OutputCacheItem data, int duration, List<AppState> appStates, IList<string> appPaths = null)
        {
            try
            {
                // Never store 0, that's like never-expire
                if (duration == 0) duration = 1;
                var expiration = new TimeSpan(0, 0, duration);
                var policy = new CacheItemPolicy { SlidingExpiration = expiration };
                // get new instance of ChangeMonitor and insert it to the cache item
                if (appStates.Any())
                    foreach(var appState in appStates)
                        policy.ChangeMonitors.Add(new AppResetMonitor(appState));

                if (appPaths != null && appPaths.Count > 0)
                    policy.ChangeMonitors.Add(new FolderChangeMonitor(appPaths));

                Cache.Set(new CacheItem(cacheKey, data), policy);
                return cacheKey;
            }
            catch
            {
                /* ignore for now */
            }

            return "error";
        }

        public OutputCacheItem Get(string key) => Cache[key] as OutputCacheItem;

        private static MemoryCache Cache => MemoryCache.Default;

    }
}