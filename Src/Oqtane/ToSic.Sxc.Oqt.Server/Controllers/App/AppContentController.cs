﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Oqtane.Repository;
using ToSic.Eav.Data;
using ToSic.Eav.Run;
using ToSic.Eav.WebApi;
using ToSic.Sxc.Oqt.Server.Repository;
using ToSic.Sxc.Oqt.Shared;
using ToSic.Sxc.WebApi.App;


// TODO: #MissingFeature not yet implemented create/update/delete
// TODO: #MissingFeature not yet implemented GetContext using current context

namespace ToSic.Sxc.Oqt.Server.Controllers
{
    [ApiController]
    [Route( WebApiConstants.WebApiStateRoot + "/app/{appPath}/content/")]
    public class AppContentController: SxcStatefulControllerBase
    {
        #region DI / Constructor
        protected override string HistoryLogName => WebApiConstants.MvcApiLogPrefix + "AppCnt";

        public AppContentController(SxcOqtane sxcOqtane, IZoneMapper zoneMapper, ITenantResolver tenantResolver, IUserResolver userResolver) :
            base(sxcOqtane, zoneMapper, tenantResolver, userResolver) { }
        #endregion

        //private IInstanceContext GetContext()
        //{
        //    // in case the initial request didn't yet find a block builder, we need to create it now
        //    var context = // BlockBuilder?.Context ??
        //                  new InstanceContext(new MvcTenant(), new PageNull(), new ContainerNull(), new MvcUser());
        //    return context;
        //}

        #region Get List / all of a certain content-type
        /// <summary>
        /// Get all Entities of specified Type
        /// </summary>
        [HttpGet("{contentType}")]
        [AllowAnonymous]   // will check security internally, so assume no requirements
        public IEnumerable<Dictionary<string, object>> GetEntities(string contentType, string appPath = null)
            => Eav.Factory.Resolve<AppContent>().Init(GetContext(), NoBlock, Log).GetItems(contentType, appPath);

        #endregion


        #region GetOne by ID / GUID

        [HttpGet("{contentType}/{id}")]
        [AllowAnonymous] // will check security internally, so assume no requirements
        public Dictionary<string, object> GetOne(string contentType, string id, string appPath = null)
        {
            if(int.TryParse(id, out var intId))
                return GetAndSerializeOneAfterSecurityChecks(contentType,
                    entityApi => entityApi.GetOrThrow(contentType, intId), appPath);

            if (Guid.TryParse(id, out var guid))
                return GetAndSerializeOneAfterSecurityChecks(contentType,
                    entityApi => entityApi.GetOrThrow(contentType, guid), appPath);

            throw new Exception("id neither int/guid, can't process");
        }


        /// <summary>
        /// Preprocess security / context, then get the item based on an passed in method,
        /// ...then process/finish
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="getOne"></param>
        /// <param name="appPath"></param>
        /// <returns></returns>
        private Dictionary<string, object> GetAndSerializeOneAfterSecurityChecks(string contentType, Func<EntityApi, IEntity> getOne, string appPath)
            => Eav.Factory.Resolve<AppContent>().Init(GetContext(), NoBlock, Log).GetOne(contentType, getOne, appPath);

        #endregion

    }
}