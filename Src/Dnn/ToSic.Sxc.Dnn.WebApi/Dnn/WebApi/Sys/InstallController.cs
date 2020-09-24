﻿using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotNetNuke.Security;
using DotNetNuke.Web.Api;
using ToSic.Sxc.Dnn.Run;
using ToSic.Sxc.Run;
using ToSic.Sxc.WebApi.ImportExport;

namespace ToSic.Sxc.Dnn.WebApi.Sys
{
    public class InstallController : DnnApiControllerWithFixes
    {
        protected override string HistoryLogName => "Api.Install";

        /// <summary>
        /// Make sure that these requests don't land in the normal api-log.
        /// Otherwise each log-access would re-number what item we're looking at
        /// </summary>
        protected override string HistoryLogGroup { get; } = "web-api.install";

        #region System Installation

        /// <summary>
        /// Finish system installation which had somehow been interrupted
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Host)]
        public bool Resume() => Eav.Factory.Resolve<IEnvironmentInstaller>().ResumeAbortedUpgrade();

        #endregion


        #region App / Content Package Installation

        /// <summary>
        /// Before this was GET Module/RemoteInstallDialogUrl 
        /// </summary>
        /// <param name="isContentApp"></param>
        /// <returns></returns>
        [HttpGet]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Admin)]
        public string RemoteWizardUrl(bool isContentApp) =>
            Eav.Factory.Resolve<IEnvironmentInstaller>().Init(Log)
                .GetAutoInstallPackagesUiUrl(
                    new DnnTenant(PortalSettings),
                    new DnnContainer().Init(Request.FindModuleInfo(), Log), 
                    isContentApp);


        /// <summary>
        /// Before this was GET Installer/InstallPackage
        /// </summary>
        /// <param name="packageUrl"></param>
        /// <returns></returns>
        [HttpPost]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Admin)]
        [ValidateAntiForgeryToken] // now activate this, as it's post now, previously not, because this is a GET and can't include the RVT
        public HttpResponseMessage RemotePackage(string packageUrl)
        {
            PreventServerTimeout300();

            Log.Add("install package:" + packageUrl);
            var container = new DnnContainer().Init(ActiveModule, Log);
            var block = container.BlockIdentifier;

            var result = Eav.Factory.Resolve<ImportFromRemote>().Init(new DnnUser(UserInfo), Log)
                .InstallPackage(block.ZoneId, block.AppId, ActiveModule.DesktopModule.ModuleName == "2sxc-app", packageUrl);

            Log.Add("install completed with success:" + result.Item1);
            return Request.CreateResponse(result.Item1 ? HttpStatusCode.OK : HttpStatusCode.InternalServerError, new { result.Item1, result.Item2 });
        }

        #endregion
    }
}