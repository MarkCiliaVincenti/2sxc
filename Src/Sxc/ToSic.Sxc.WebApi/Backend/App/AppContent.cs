﻿using System.Text.Json;
using System.Text.Json.Nodes;
using ToSic.Eav.Apps.Internal.Api01;
using ToSic.Eav.Apps.State;
using ToSic.Eav.DataFormats.EavLight;
using ToSic.Eav.Generics;
using ToSic.Eav.Metadata;
using ToSic.Eav.Plumbing;
using ToSic.Eav.Security;
using ToSic.Eav.Security.Internal;
using ToSic.Eav.WebApi;
using ToSic.Eav.WebApi.App;
using ToSic.Eav.WebApi.Errors;
using ToSic.Sxc.Data.Internal.Convert;
using static ToSic.Eav.Apps.Internal.Api01.SaveApiAttributes;

namespace ToSic.Sxc.Backend.App;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class AppContent : ServiceBase
{
    private readonly GenWorkDb<WorkFieldList> _workFieldList;
    private readonly Generator<MultiPermissionsTypes> _typesPermissions;
    private readonly Generator<MultiPermissionsItems> _itemsPermissions;

    #region Constructor / DI

    public AppContent(
        EntityApi entityApi,
        LazySvc<IConvertToEavLight> entToDicLazy,
        ISxcContextResolver ctxResolver,
        Generator<MultiPermissionsTypes> typesPermissions,
        Generator<MultiPermissionsItems> itemsPermissions,
        GenWorkDb<WorkFieldList> workFieldList,
        LazySvc<SimpleDataEditService> dataControllerLazy) : base("Sxc.ApiApC")
    {
        ConnectServices(
            _workFieldList = workFieldList,
            _entityApi = entityApi,
            _entToDicLazy = entToDicLazy,
            _ctxResolver = ctxResolver,
            _typesPermissions = typesPermissions,
            _itemsPermissions = itemsPermissions,
            _dataControllerLazy = dataControllerLazy
        );
    }

    private readonly EntityApi _entityApi;
    private readonly LazySvc<IConvertToEavLight> _entToDicLazy;
    private readonly ISxcContextResolver _ctxResolver;
    private readonly LazySvc<SimpleDataEditService> _dataControllerLazy;

    public AppContent Init(string appName)
    {
        // if app-path specified, use that app, otherwise use from context
        Context = _ctxResolver.AppNameRouteBlock(appName);
        return this;
    }
    protected IContextOfApp Context;

    protected IAppStateInternal AppState => Context?.AppState ??
                                   throw new(
                                       "Can't access AppState before Context is ready. Did you forget to call Init(...)?");

    #endregion


    #region Get Items

    public IEnumerable<IDictionary<string, object>> GetItems(string contentType, string appPath = default, string oDataSelect = default)
    {
        var wrapLog = Log.Fn<IEnumerable<IDictionary<string, object>>>($"get entities type:{contentType}, path:{appPath}");

        // verify that read-access to these content-types is permitted
        var permCheck = ThrowIfNotAllowedInType(contentType, GrantSets.ReadSomething, AppState);

        var includeDrafts = permCheck.EnsureAny(GrantSets.ReadDraft);
        var result = _entityApi.GetEntities(AppState, contentType, includeDrafts, oDataSelect)
            ?.ToList();
        return wrapLog.Return(result, "found: " + result?.Count);
    }


    #endregion

    #region Get One 

    /// <summary>
    /// Preprocess security / context, then get the item based on an passed in method, 
    /// ...then process/finish
    /// </summary>
    /// <returns></returns>
    public IDictionary<string, object> GetOne(string contentType, Func<IEnumerable<IEntity>, IEntity> getOne, 
        string appPath, string oDataSelect)
    {
        Log.A($"get and serialize after security check type:{contentType}, path:{appPath}");

        // first try to find in all entities incl. drafts
        var itm = getOne(AppState.List);
        var permCheck = ThrowIfNotAllowedInItem(itm, GrantSets.ReadSomething, AppState);

        // in case draft wasn't allow, get again with more restricted permissions 
        if (!permCheck.EnsureAny(GrantSets.ReadDraft))
            itm = getOne(AppState.ListPublished);

        return InitEavAndSerializer(AppState.AppId, Context.UserMayEdit, oDataSelect).Convert(itm);
    }


    #endregion

    #region CreateOrUpdate


    public IDictionary<string, object> CreateOrUpdate(string contentType, Dictionary<string, object> newContentItem, int? id = null, string appPath = null)
    {
        Log.A($"create or update type:{contentType}, id:{id}, path:{appPath}");

        // if app-path specified, use that app, otherwise use from context

        // Check that this ID is actually of this content-type,
        // this throws an error if it's not the correct type
        var itm = id == null
            ? null
            : AppState.List.GetOrThrow(contentType, id.Value);

        if (itm == null) ThrowIfNotAllowedInType(contentType, GrantSets.CreateSomething, AppState);
        else ThrowIfNotAllowedInItem(itm, GrantSets.WriteSomething, AppState);

        // Convert to case-insensitive dictionary just to be safe!
        var rawValuesCaseInsensitive = newContentItem.ToInvariant();

        // Now create the cleaned up import-dictionary so we can create a new entity
        var cleanedNewItem = new AppContentEntityBuilder(Log)
            .CreateEntityDictionary(contentType, rawValuesCaseInsensitive, AppState)
            .ToInvariant();

        // add owner
        if (!cleanedNewItem.ContainsKey(Attributes.EntityFieldOwner))
            cleanedNewItem.Add(Attributes.EntityFieldOwner, Context.User.IdentityToken);

        var dataController = DataController(AppState);
        if (id == null)
        {
            // Get Metadata - not sure why we're using the raw values, but maybe there were removed in cleaned?
            Log.A($"create new entity because id is null");
            var metadata = GetMetadata(rawValuesCaseInsensitive);
            Log.A($"metadata: {metadata}");

            var ids = dataController.Create(contentType, new List<Dictionary<string, object>> { cleanedNewItem }, metadata);
            id = ids.FirstOrDefault();

            Log.A($"new entity id: {id}");
            // Get Metadata - not sure why we're using the raw values, but maybe there were removed in cleaned?
            var added = AddParentRelationship(rawValuesCaseInsensitive, id.Value);
        }
        else
            dataController.Update(id.Value, cleanedNewItem);

        return InitEavAndSerializer(AppState.AppId, Context.UserMayEdit, null)
            .Convert(AppState.List.One(id.Value));
    }

    private bool AddParentRelationship(IDictionary<string, object> valuesCaseInsensitive, int addedEntityId)
    {
        var wrapLog = Log.Fn<bool>($"item dictionary key count: {valuesCaseInsensitive.Count}");

        if (!valuesCaseInsensitive.Keys.Contains(ParentRelationship))
            return wrapLog.ReturnFalse($"'{ParentRelationship}' key is missing");

        var objectOrNull = valuesCaseInsensitive[ParentRelationship];
        if (objectOrNull == null) 
            return wrapLog.ReturnFalse($"'{ParentRelationship}' value is null");

        if (objectOrNull is not JsonObject parentRelationship)
            return wrapLog.ReturnNull($"'{ParentRelationship}' value is not JsonObject");

        var parentGuid = (Guid?)parentRelationship[ParentRelParent];
        if (!parentGuid.HasValue) 
            return wrapLog.ReturnFalse($"'{ParentRelParent}' guid is missing");

        var parentEntity = AppState.GetDraftOrPublished(parentGuid.Value);
        if (parentEntity == null) 
            return wrapLog.ReturnFalse("Parent entity is missing");

        var ids = new[] { addedEntityId as int? };
        var index = (int)parentRelationship[ParentRelIndex];

        var field = (string)parentRelationship[ParentRelField];
        var fields = new[] { field };

        _workFieldList.New(AppState).FieldListAdd(parentEntity, fields, index, ids, asDraft: false, forceAddToEnd: false);

        return wrapLog.ReturnTrue($"new ParentRelationship p:{parentGuid},f:{field},i:{index}");
    }

    private Target GetMetadata(Dictionary<string, object> newContentItemCaseInsensitive) => Log.Func($"count: {newContentItemCaseInsensitive.Count}", () =>
    {
        if (!newContentItemCaseInsensitive.Keys.Contains(Attributes.JsonKeyMetadataFor))
            return (null, $"'{Attributes.JsonKeyMetadataFor}' key is missing");

        var objectOrNull = newContentItemCaseInsensitive[Attributes.JsonKeyMetadataFor];
        if (objectOrNull == null) 
            return (null, $"'{Attributes.JsonKeyMetadataFor}' value is null");

        if (objectOrNull is not JsonObject metadataFor)
            return (null, $"'{Attributes.JsonKeyMetadataFor}' value is not JsonObject");

        var metaData = new Target(GetTargetType(metadataFor[Attributes.TargetNiceName]?.AsValue()), null,
            
            keyGuid: (Guid?)metadataFor[Attributes.GuidNiceName],
            keyNumber: (int?)metadataFor[Attributes.NumberNiceName],
            keyString: (string)metadataFor[Attributes.StringNiceName]
        );
        return (metaData, $"new metadata g:{metaData.KeyGuid},n:{metaData.KeyNumber},s:{metaData.KeyString}");

    });

    private static int GetTargetType(JsonValue target)
    {
        switch (target.GetValue<JsonElement>().ValueKind)
        {
            case JsonValueKind.Number:
                return (int)target;
            case JsonValueKind.String when Enum.TryParse<TargetTypes>((string)target, out var targetTypes):
                return (int)targetTypes;
            default:
                throw new ArgumentOutOfRangeException(Attributes.TargetNiceName, "Value is not 'int' or TargetTypes 'string'.");
        }
    }

    private SimpleDataEditService DataController(IAppIdentity app) => _dataController ??= _dataControllerLazy.Value.Init(app.ZoneId, app.AppId);
    private SimpleDataEditService _dataController;

    #endregion

    #region helpers / initializers to prep the EAV and Serializer

    private IConvertToEavLight InitEavAndSerializer(int appId, bool userMayEdit, string oDataSelect)
    {
        var l = Log.Fn<IConvertToEavLight>($"init eav for a#{appId}");
        // Improve the serializer so it's aware of the 2sxc-context (module, portal etc.)
        var ser = _entToDicLazy.Value;
        ser.WithGuid = true;
        var converter = (ConvertToEavLightWithCmsInfo)ser;
        converter.WithEdit = userMayEdit;
        if (oDataSelect.HasValue())
            converter.AddSelectFields([.. oDataSelect.CsvToArrayWithoutEmpty()]);

        return l.Return(ser);
    }
    #endregion


    #region Delete

    public void Delete(string contentType, int id, string appPath)
    {
        var l = Log.Fn($"id:{id}, type:{contentType}, path:{appPath}");
        // Note: if app-path specified, use that app, otherwise use from context - probably automatic based on headers?

        // don't allow type "any" on this
        if (contentType == "any")
            throw l.Done(new Exception("type any not allowed with id-only, requires guid"));

        var entityApi = _entityApi.Init(AppState.AppId);
        var itm = AppState.List.GetOrThrow(contentType, id);
        ThrowIfNotAllowedInItem(itm, Grants.Delete.AsSet(), AppState);
        entityApi.Delete(itm.Type.Name, id);
        l.Done();
    }

    public void Delete(string contentType, Guid guid, string appPath)
    {
        var l = Log.Fn($"guid:{guid}, type:{contentType}, path:{appPath}");
        // Note: if app-path specified, use that app, otherwise use from context - probably automatic based on headers?

        var entityApi = _entityApi.Init(AppState.AppId);
        var itm = AppState.List.GetOrThrow(contentType == "any" ? null : contentType, guid);

        ThrowIfNotAllowedInItem(itm, Grants.Delete.AsSet(), AppState);

        entityApi.Delete(itm.Type.Name, guid);
        l.Done();
    }


    #endregion

    #region Permission Checks

    protected MultiPermissionsTypes ThrowIfNotAllowedInType(string contentType, List<Grants> requiredGrants, IAppIdentity appIdentity)
    {
        var permCheck = _typesPermissions.New().Init(Context, appIdentity, contentType);
        if (!permCheck.EnsureAll(requiredGrants, out var error))
            throw HttpException.PermissionDenied(error);
        return permCheck;
    }

    protected MultiPermissionsItems ThrowIfNotAllowedInItem(IEntity itm, List<Grants> requiredGrants, IAppIdentity appIdentity)
    {
        var permCheck = _itemsPermissions.New().Init(Context, appIdentity, itm);
        if (!permCheck.EnsureAll(requiredGrants, out var error))
            throw HttpException.PermissionDenied(error);
        return permCheck;
    }

    #endregion
}