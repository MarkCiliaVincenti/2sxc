﻿using System;
using System.Web.Http;
using DotNetNuke.Security;
using DotNetNuke.Security.Permissions;
using DotNetNuke.Services.FileSystem;
using DotNetNuke.Web.Api;
using ToSic.Sxc.Dnn.Code;
using ToSic.Sxc.WebApi;
using ToSic.Sxc.WebApi.Cms;

namespace ToSic.Sxc.Dnn.WebApi.Cms
{
	[SupportedModules("2sxc,2sxc-app")]
    [ValidateAntiForgeryToken]
	public class LinkController : SxcApiControllerBase
	{
        protected override string HistoryLogName => "Api.LnkCnt";

        [HttpGet]
		[DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
		public object GetFileByPath(string relativePath)
        {
            var dnnDynamicCode = new DnnDynamicCodeRoot().Init(GetBlock(), Log);
            var portal = dnnDynamicCode.Dnn.Portal;
            relativePath = relativePath.Replace(portal.HomeDirectory, "");
			var file = FileManager.Instance.GetFile(portal.PortalId, relativePath);
            if (file == null) return null;
            var folder = (FolderInfo)FolderManager.Instance.GetFolder(file.FolderId);
            return FolderPermissionController.CanViewFolder(folder)
                ? new
                {
                    file.FileId
                }
                : null;
        }

        /// <summary>
        /// This overload is only for resolving page-references, which need fewer parameters
        /// </summary>
        /// <returns></returns>
	    [HttpGet]
	    [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.View)]
	    public string Resolve(string hyperlink, int appId)
            => Resolve(hyperlink, appId, null, default, null);

	    [HttpGet]
		[DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.View)]
		public string Resolve(string hyperlink, int appId, string contentType, Guid guid, string field) 
            => Eav.Factory.Resolve</*>() new */HyperlinkBackend<int, int>>().Init(Log).ResolveHyperlink(GetBlock(), hyperlink, appId, contentType, guid, field);
    }
}