﻿using System.IO;
using ToSic.Eav.Apps;
using ToSic.Eav.Apps.Integration;
using ToSic.Eav.Apps.Internal.Work;
using ToSic.Eav.Context;
using ToSic.Eav.ImportExport.Internal.Zip;
using ToSic.Lib.DI;
using ToSic.Lib.Services;

namespace ToSic.Sxc.Apps.Internal.Work;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class WorkAppsRemove: ServiceBase 
{
    #region Constructor / DI

    public WorkAppsRemove(LazySvc<ZoneManager> zoneManagerLazy, IAppStates appStates, ISite site, IAppPathsMicroSvc appPaths) : base("Cms.AppsRt")
    {
        ConnectServices(
            _zoneManagerLazy = zoneManagerLazy,
            _appStates = appStates,
            _site = site,
            _appPaths = appPaths
        );
    }
    private readonly LazySvc<ZoneManager> _zoneManagerLazy;
    private readonly IAppStates _appStates;
    private readonly ISite _site;
    private readonly IAppPathsMicroSvc _appPaths;

    #endregion


    internal void RemoveAppInSiteAndEav(int zoneId, int appId, bool fullDelete)
    {
        // check portal assignment and that it's not the default app
        // enable restore for DefaultApp
        if (appId == _appStates.DefaultAppId(zoneId) && fullDelete)
            throw new("The default app of a zone cannot be removed.");

        if (appId == Eav.Constants.MetaDataAppId)
            throw new("The special old global app cannot be removed.");

        // todo: maybe verify the app is of this portal; I assume delete will fail anyhow otherwise

        // Prepare to Delete folder in dnn - this must be done, before deleting the app in the DB
        var appState = _appStates.GetReader(new AppIdentity(zoneId, appId));
        var paths = _appPaths.Init(_site, appState);
        var folder = appState.Folder;
        var physPath = paths.PhysicalPath;

        // now remove from DB. This sometimes fails, so we do this before trying to clean the files
        // as the db part should be in a transaction, and if it fails, everything should stay as is
        _zoneManagerLazy.Value.SetId(zoneId).DeleteApp(appId, fullDelete);

        // now really delete the files - if the DB didn't end up throwing an error
        // ...but only if it's a full-delete
        if (fullDelete && !string.IsNullOrEmpty(folder) && Directory.Exists(physPath))
            ZipImport.TryToDeleteDirectory(physPath, Log);
    }
}