﻿using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using ToSic.Eav.DataSources.Queries;
using ToSic.Eav.Documentation;
using ToSic.Eav.Logging;
using ToSic.Sxc.Dnn.Run;

// ReSharper disable once CheckNamespace
namespace ToSic.Sxc.DataSources
{
    /// <summary>
    /// Deliver a list of users from the current platform (Dnn or Oqtane)
    /// </summary>
    [PrivateApi("hide internal implementation")]
    [VisualQuery(
        NiceName = VqNiceName,
        Icon = VqIcon,
        UiHint = VqUiHint,
        HelpLink = VqHelpLink,
        GlobalName = VqGlobalName,
        Type = VqType,
        ExpectsDataOfType = VqExpectsDataOfType,
        Difficulty = DifficultyBeta.Default
    )]
    public class DnnUsers : Users
    {
        protected override IEnumerable<UserDataSourceInfo> GetUsersInternal()
        {
            var wrapLog = Log.Fn<List<UserDataSourceInfo>>();
            var siteId = PortalSettings.Current?.PortalId ?? -1;
            Log.A($"Portal Id {siteId}");
            try
            {
                // take all portal users (this should include superusers, but superusers are missing)
                var dnnAllUsers = UserController.GetUsers(portalId: siteId, includeDeleted: false, superUsersOnly: false);
                
                // append all superusers
                dnnAllUsers.AddRange(UserController.GetUsers(portalId: -1, includeDeleted: false, superUsersOnly: true));
                
                var dnnUsers = dnnAllUsers.Cast<UserInfo>().ToList();
                if (!dnnUsers.Any()) return wrapLog.Return(new List<UserDataSourceInfo>(), "null/empty");

                var result = dnnUsers
                    //.Where(d => !d.IsDeleted)
                    .Select(d =>
                    {
                        var adminInfo = d.UserMayAdminThis();
                        return new UserDataSourceInfo
                        {
                            Id = d.UserID,
                            Guid = d.UserGuid(),
                            NameId = d.UserIdentityToken(),
                            RoleIds = d.RoleList(portalId: siteId),
                            IsSystemAdmin = d.IsSuperUser,
                            IsSiteAdmin = adminInfo.IsSiteAdmin,
                            IsContentAdmin = adminInfo.IsContentAdmin,
                            //IsDesigner = d.IsDesigner(),
                            IsAnonymous = d.IsAnonymous(),
                            Created = d.CreatedOnDate,
                            Modified = d.LastModifiedOnDate,
                            //
                            Username = d.Username,
                            Email = d.Email,
                            Name = d.DisplayName
                        };
                    }).ToList();
                return wrapLog.Return(result, "found");
            }
            catch (Exception ex)
            {
                Log.Ex(ex);
                return wrapLog.Return(new List<UserDataSourceInfo>(), "error");
            }
        }
    }
}