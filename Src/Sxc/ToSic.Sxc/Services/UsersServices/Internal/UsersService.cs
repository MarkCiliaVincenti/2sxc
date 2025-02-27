﻿using ToSic.Eav.Context;
using ToSic.Eav.Plumbing;
using ToSic.Lib.DI;
using ToSic.Sxc.Context;
using ToSic.Sxc.Context.Internal.Raw;
using ToSic.Sxc.Internal;
using static System.StringComparison;

namespace ToSic.Sxc.Services.Internal;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class UsersService : ServiceForDynamicCode, IUserService
{
    private readonly UserSourceProvider _provider;

    public UsersService(LazySvc<IContextOfSite> context, UserSourceProvider provider) : base($"{SxcLogName}.UsrInfoSrv")
    {
        ConnectServices(
            _context = context,
            _provider = provider
        );
    }

    private readonly LazySvc<IContextOfSite> _context;

    //private string PlatformIdentityTokenPrefix => _provider.PlatformIdentityTokenPrefix;

    //private ICmsUser PlatformUserInformationDto(int userId) => _provider.PlatformUserInformationDto(userId, SiteId);

    public ICmsUser Get(string identityToken)
    {
        var l = Log.Fn<ICmsUser>($"token:{identityToken}");

        var userId = UserId(identityToken);

        return l.Return(Get(userId));
    }

    public ICmsUser Get(int userId) 
    {
        var l = Log.Fn<ICmsUser>($"id:{userId}");

        if (userId == CmsUserRaw.AnonymousUser.Id)
            return l.ReturnAsOk(CmsUserRaw.AnonymousUser);

        if (userId == CmsUserRaw.UnknownUser.Id)
            return l.Return(CmsUserRaw.UnknownUser, "err");

        var userDto = _provider.PlatformUserInformationDto(userId, SiteId);

        return userDto != null
            ? l.ReturnAsOk(userDto)
            : l.Return(CmsUserRaw.UnknownUser, "err");
    }

    /// <summary>
    /// Helper method to get SiteId.
    /// </summary>
    private int SiteId => _context.Value.Site.Id;

    /// <summary>
    /// Helper method to parse UserID from user identity token.
    /// </summary>
    /// <param name="identityToken"></param>
    /// <returns></returns>
    private int UserId(string identityToken) 
    {
        var l = Log.Fn<int>($"token:{identityToken}");

        if (string.IsNullOrWhiteSpace(identityToken))
            return l.Return(CmsUserRaw.UnknownUser.Id, "empty identity token");

        if (identityToken.EqualsInsensitive(SxcUserConstants.Anonymous))
            return l.Return(CmsUserRaw.AnonymousUser.Id, "ok (anonymous)");

        var prefix = _provider.PlatformIdentityTokenPrefix;
        if (identityToken.StartsWith(prefix, InvariantCultureIgnoreCase))
            identityToken = identityToken.Substring(prefix.Length);

        return int.TryParse(identityToken, out var userId)
            ? l.Return(userId, $"ok (u:{userId})")
            : l.Return(CmsUserRaw.UnknownUser.Id, "err");
    }
}