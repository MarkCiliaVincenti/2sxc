﻿using ToSic.Lib.Helpers;
using ToSic.Razor.Markup;
using ToSic.Sxc.Web.Internal.ClientAssets;
using ToSic.Sxc.Web.Internal.ContentSecurityPolicy;
using ToSic.Sxc.Web.Internal.PageFeatures;
using ToSic.Sxc.Web.Internal.PageService;

namespace ToSic.Sxc.Blocks.Internal.Render;

/// <inheritdoc />
[PrivateApi]
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class RenderResult(string html) : TagText(null), IRenderResult
{
    /// <inheritdoc />
    public string Html { get; set; } = html;

    public int Size => _size.Get(() => Html?.Length ?? 0);
    private readonly GetOnce<int> _size = new();

    /// <inheritdoc />
    public bool CanCache { get; set; }

    /// <inheritdoc />
    public bool IsError { get; set; }

    /// <inheritdoc />
    public IList<IPageFeature> Features { get; set; }

    /// <inheritdoc />
    public IList<IClientAsset> Assets { get; set; }

    /// <inheritdoc />
    public IList<PagePropertyChange> PageChanges { get; set; }

    /// <inheritdoc />
    public IList<HeadChange> HeadChanges { get; set; }

    /// <inheritdoc />
    public IList<IPageFeature> FeaturesFromSettings { get; set; }

    /// <inheritdoc />
    public int? HttpStatusCode { get; set; }

    /// <inheritdoc />
    public string HttpStatusMessage { get; set; }

    /// <inheritdoc />
    public List<IDependentApp> DependentApps { get; } = [];


    public int ModuleId { get; set; }

    public override string ToString() => Html;

    public IList<HttpHeader> HttpHeaders { get; set; }

    public bool CspEnabled { get; set; } = false;
    public bool CspEnforced { get; set; } = false;
    public IList<CspParameters> CspParameters { get; set; }

    public List<string> Errors { get; set; }
}