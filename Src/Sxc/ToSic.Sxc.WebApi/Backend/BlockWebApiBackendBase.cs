﻿using ToSic.Eav.Security;
using ToSic.Eav.Security.Internal;
using ToSic.Eav.WebApi.Errors;
using ToSic.Sxc.Blocks.Internal;

namespace ToSic.Sxc.Backend;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public abstract class BlockWebApiBackendBase(
    Generator<MultiPermissionsApp> multiPermissionsApp,
    AppWorkContextService appWorkCtxService,
    ISxcContextResolver ctxResolver,
    string logName,
    object[] connect = default)
    : ServiceBase(logName, connect: [..connect ?? [], multiPermissionsApp, ctxResolver, appWorkCtxService])
{
    public AppWorkContextService AppWorkCtxService { get; } = appWorkCtxService;
    public ISxcContextResolver CtxResolver { get; } = ctxResolver;

    protected IContextOfApp ContextOfBlock =>
        _contextOfAppOrBlock ??= CtxResolver.BlockContextRequired();
    private IContextOfApp _contextOfAppOrBlock;

    #region Block-Context Requiring properties

    public IBlock Block => _block ??= CtxResolver.BlockRequired();
    private IBlock _block;

    protected IAppWorkCtx AppWorkCtx => _appWorkCtx ??= AppWorkCtxService.Context(Block.Context.AppState);
    private IAppWorkCtx _appWorkCtx;
    protected IAppWorkCtxPlus AppWorkCtxPlus => _appWorkCtxPlus ??= AppWorkCtxService.ToCtxPlus(AppWorkCtx);
    private IAppWorkCtxPlus _appWorkCtxPlus;
    protected IAppWorkCtxWithDb AppWorkCtxDb => _appWorkCtxDb ??= AppWorkCtxService.CtxWithDb(AppWorkCtx.AppState);
    private IAppWorkCtxWithDb _appWorkCtxDb;

    #endregion


    protected void ThrowIfNotAllowedInApp(List<Grants> requiredGrants, IAppIdentity alternateApp = null)
    {
        var permCheck = multiPermissionsApp.New().Init(ContextOfBlock, alternateApp ?? ContextOfBlock.AppState);
        if (!permCheck.EnsureAll(requiredGrants, out var error))
            throw HttpException.PermissionDenied(error);
    }
}