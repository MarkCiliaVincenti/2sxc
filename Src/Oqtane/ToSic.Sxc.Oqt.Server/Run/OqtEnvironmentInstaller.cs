﻿using System.Linq;
using ToSic.Eav.Apps;
using ToSic.Eav.Apps.Internal.Work;
using ToSic.Eav.Context;
using ToSic.Lib.Services;
using ToSic.Sxc.Apps.Internal.Work;
using ToSic.Sxc.Context;
using ToSic.Sxc.Integration.Installation;
using ToSic.Sxc.Oqt.Shared;

namespace ToSic.Sxc.Oqt.Server.Run;

internal class OqtEnvironmentInstaller: ServiceBase, IEnvironmentInstaller, IPlatformAppInstaller
{
    private readonly GenWorkPlus<WorkViews> _workViews;
    private readonly RemoteRouterLink _remoteRouterLink;
    private readonly IAppStates _appStates;

    public OqtEnvironmentInstaller(GenWorkPlus<WorkViews> workViews, RemoteRouterLink remoteRouterLink, IAppStates appStates) : base($"{OqtConstants.OqtLogPrefix}.Instll")
    {
        ConnectServices(
            _remoteRouterLink = remoteRouterLink,
            _workViews = workViews,
            _appStates = appStates
        );
    }


    public string UpgradeMessages()
    {
        // for now, always assume installation worked
        return null;
    }

    private bool IsUpgradeRunning => false;

    public bool ResumeAbortedUpgrade()
    {
        // don't do anything for now
        return true;
    }

    public string GetAutoInstallPackagesUiUrl(ISite site, IModule module, bool forContentApp)
    {

        // new: check if it should allow this
        // it should only be allowed, if the current situation is either
        // Content - and no views exist (even invisible ones)
        // App - and no apps exist - this is already checked on client side, so I won't include a check here
        if (forContentApp)
            try
            {
                var contentAppId = _appStates.IdentityOfDefault(site.ZoneId);
                // we'll usually run into errors if nothing is installed yet, so on errors, we'll continue
                var contentViews = _workViews.New(contentAppId).GetAll();
                if (contentViews.Any()) return null;
            }
            catch { /* ignore */ }

        var link = _remoteRouterLink.LinkToRemoteRouter(
            RemoteDestinations.AutoConfigure,
            site,
            module.Id,
            app: null,
            forContentApp);

        return link;
    }

}