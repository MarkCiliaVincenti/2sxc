﻿// #RemoveOldEntityPicker - commented out 2024-03-05, remove ca. 2024-06-01
//using ToSic.Eav.Security.Internal;
//using ToSic.Eav.WebApi;
//using ToSic.Eav.WebApi.Errors;

//namespace ToSic.Sxc.Backend.Cms;

//[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
//public class EntityPickerBackend: ServiceBase
//{
//    #region DI Constructor

//    public EntityPickerBackend(EntityPickerApi entityPickerApi,
//        ISxcContextResolver ctxResolver,
//        Generator<MultiPermissionsApp> appPermissions,
//        Generator<MultiPermissionsTypes> typePermissions) : base("BE.EntPck")
//    {
//        ConnectServices(
//            _entityPickerApi = entityPickerApi,
//            _ctxResolver = ctxResolver,
//            _appPermissions = appPermissions,
//            _typePermissions = typePermissions
//        );
//    }
//    private readonly EntityPickerApi _entityPickerApi;
//    private readonly ISxcContextResolver _ctxResolver;
//    private readonly Generator<MultiPermissionsApp> _appPermissions;
//    private readonly Generator<MultiPermissionsTypes> _typePermissions;

//    #endregion

//    // 2dm 2023-01-22 #maybeSupportIncludeParentApps
//    public IEnumerable<EntityForPickerDto> GetForEntityPicker(int appId, string[] items, string contentTypeName) => Log.Func(() =>
//    {
//        var context = _ctxResolver.GetBlockOrSetApp(appId);
//        // do security check
//        var permCheck = string.IsNullOrEmpty(contentTypeName)
//            ? _appPermissions.New().Init(context, context.AppState)
//            : _typePermissions.New().Init(context, context.AppState, contentTypeName);
//        if (!permCheck.EnsureAll(GrantSets.ReadSomething, out var error))
//            throw HttpException.PermissionDenied(error);

//        // maybe in the future, ATM not relevant
//        var withDrafts = permCheck.EnsureAny(GrantSets.ReadDraft);

//        return _entityPickerApi.GetForEntityPicker(appId, items, contentTypeName, withDrafts);
//    });
//}