﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oqtane.Shared;
using System;
using System.Collections.Generic;
using ToSic.Eav.Context;
using ToSic.Sxc.Oqt.Server.Controllers;
using ToSic.Sxc.Oqt.Server.Installation;
using ToSic.Sxc.Oqt.Shared;
using ToSic.Sxc.Run;
using ToSic.Sxc.WebApi.ImportExport;
using ToSic.Sxc.WebApi.Licenses;

namespace ToSic.Sxc.Oqt.Server.WebApi.Sys
{
    // Release routes
    [Route(WebApiConstants.ApiRoot + "/sys/[controller]/[action]")]
    [Route(WebApiConstants.ApiRoot2 + "/sys/[controller]/[action]")]
    [Route(WebApiConstants.ApiRoot3 + "/sys/[controller]/[action]")]

    // Beta routes
    [Route(WebApiConstants.WebApiStateRoot + "/sys/[controller]/[action]")]
    public class LicenseController : OqtStatefulControllerBase
    {
        private readonly Lazy<LicenseBackend> _licenseBackendLazy;

        public LicenseController(Lazy<LicenseBackend> licenseBackendLazy )
        {
            _licenseBackendLazy = licenseBackendLazy;
        }

        protected override string HistoryLogName => "Api.License";

        /// <summary>
        /// Make sure that these requests don't land in the normal api-log.
        /// Otherwise each log-access would re-number what item we're looking at
        /// </summary>
        protected override string HistoryLogGroup { get; } = "web-api.license";

        #region License

        /// <summary>
        /// Gives an array of License (sort by priority)
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        // [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Host)]
        [Authorize(Roles = RoleNames.Host)]
        public IEnumerable<LicenseDto> Summary() => _licenseBackendLazy.Value.Init(Log).Summary();

        #endregion
       
    }
}