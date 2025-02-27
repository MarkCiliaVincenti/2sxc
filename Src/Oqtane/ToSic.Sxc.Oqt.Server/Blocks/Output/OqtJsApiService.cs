﻿using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using ToSic.Eav.Plumbing;
using ToSic.Lib.Services;
using ToSic.Sxc.Context;
using ToSic.Sxc.Oqt.Server.Plumbing;
using ToSic.Sxc.Oqt.Server.WebApi;
using ToSic.Sxc.Oqt.Shared;
using ToSic.Sxc.Web.Internal.JsContext;

namespace ToSic.Sxc.Oqt.Server.Blocks.Output;

internal class OqtJsApiService(
    IAntiforgery antiForgery,
    IHttpContextAccessor http,
    JsApiCacheService jsApiCache,
    SiteStateInitializer siteStateInitializer)
    : ServiceBase("OqtJsApi", connect: [antiForgery, http, jsApiCache, siteStateInitializer]), IJsApiService
{
    public string GetJsApiJson(int? pageId = null, string siteRoot = null, string rvt = null) 
        => JsApi.JsApiJson(GetJsApi(pageId, siteRoot, rvt));

    public JsApi GetJsApi(int? pageId = null, string siteRoot = null, string rvt = null)
    {
        return jsApiCache.JsApiJson(
            platform: PlatformType.Oqtane.ToString(),
            pageId: pageId ?? -1,
            siteRoot: SiteRootFn,
            apiRoot: ApiRootFn,
            appApiRoot: SiteRootFn, // without "app/" because the UI will add that later on,
            uiRoot: UiRootFn,
            rvtHeader: Oqtane.Shared.Constants.AntiForgeryTokenHeaderName,
            rvt: RvtFn,
            dialogQuery: null);

        string SiteRootFn() => siteRoot.IsEmpty() ? OqtPageOutput.GetSiteRoot(siteStateInitializer?.InitializedState) : siteRoot;
        string ApiRootFn() => SiteRootFn() + OqtWebApiConstants.ApiRootNoLanguage + "/";
        string UiRootFn() => OqtConstants.UiRoot + "/";
        string RvtFn() => rvt.IsEmpty() && http?.HttpContext != null ? antiForgery.GetAndStoreTokens(http.HttpContext).RequestToken : rvt;
    }

}