﻿using ToSic.Eav.Apps;
using ToSic.Eav.Apps.Services;
using ToSic.Eav.Integration;
using ToSic.Eav.Plumbing;
using ToSic.Eav.Run;
using ToSic.Lib.Services;
using ToSic.Sxc.Configuration.Internal;
using ToSic.Sxc.Services;
using static ToSic.Eav.Apps.AppStackConstants;
using static ToSic.Sxc.Web.WebResources.WebResourceConstants;
using static ToSic.Sxc.Web.WebResources.WebResourceProcessor;

namespace ToSic.Sxc.Web.Internal.EditUi;

/// <summary>
/// Provide all resources (fonts, icons, etc.) needed for the edit-ui
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class EditUiResources: ServiceBase
{

    #region Constructor

    private readonly IAppStates _appStates;
    private readonly AppDataStackService _stackServiceHelper;
    private readonly IZoneMapper _zoneMapper;
    private readonly IFeaturesService _features;

    public EditUiResources(IAppStates appStates, AppDataStackService stackServiceHelper, IZoneMapper zoneMapper, IFeaturesService features) : base("Sxc.EUiRes")
    {
        ConnectServices(
            _appStates = appStates,
            _stackServiceHelper = stackServiceHelper,
            _zoneMapper = zoneMapper,
            _features = features
        );
    }

    #endregion

    #region Resources / Constants

    public const string LinkTagTemplate = "<link rel=\"stylesheet\" href=\"{0}\">";
    public const string RobotoFromGoogle = "https://fonts.googleapis.com/css2?family=Roboto:wght@300;400;500&display=swap";
    public const string RobotoFromAltCdn = "/google-fonts/roboto/fonts.css";
    public const string MaterialIconsGoogle = "https://fonts.googleapis.com/icon?family=Material+Icons|Material+Icons+Outlined";
    public const string MaterialIconsAltCdn = "/google-fonts/material-icons/fonts.css";

    #endregion

    public EditUiResourcesSpecs GetResources(bool enabled, int siteId, EditUiResourceSettings settings)
    {
        if (!enabled) return new();
        var cdnRoot = "";
        var useAltCdn = false;
        var html = "";

        if (_features.IsEnabled(SxcFeatures.CdnSourceEdit.NameId))
        {
            var zoneId = _zoneMapper.GetZoneId(siteId);
            var appPreset = _appStates.GetPrimaryReader(zoneId, Log);
            var stack = _stackServiceHelper.Init(appPreset).GetStack(RootNameSettings);
            var getResult = stack.InternalGetPath($"{WebResourcesNode}.{CdnSourceEditField}");
            cdnRoot = getResult.Result as string;
            useAltCdn = cdnRoot.HasValue() && cdnRoot != CdnDefault;
            cdnRoot += VersionSuffix;
            html += $"<!-- CDN settings {getResult.IsFinal}, '{getResult.Result}', '{getResult.Result?.GetType()}' '{cdnRoot}', {cdnRoot?.Length} -->";
        }
        else
            html += "<!-- CDN Settings: Default (feature not enabled) -->";

        if (settings.IconsMaterial)
        {
            var url = useAltCdn ? cdnRoot + MaterialIconsAltCdn : MaterialIconsGoogle;
            html += "\n" + string.Format(LinkTagTemplate, url);
        }

        if (settings.FontRoboto)
        {
            var url = useAltCdn ? cdnRoot + RobotoFromAltCdn : RobotoFromGoogle;
            html += "\n" + string.Format(LinkTagTemplate, url);
        }
        html += "\n";
        return new() { HtmlHead = html };
    }

    public class EditUiResourcesSpecs
    {
        public string HtmlHead { get; set; } = "";

        // later we'll also add CSP specs here
    }

}