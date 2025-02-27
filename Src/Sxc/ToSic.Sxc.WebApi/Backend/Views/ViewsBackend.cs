﻿using ToSic.Eav.DataFormats.EavLight;
using ToSic.Eav.Serialization;
using ToSic.Eav.WebApi.Security;
using ToSic.Sxc.Apps.Internal.Work;
using ToSic.Sxc.Backend.ImportExport;

namespace ToSic.Sxc.Backend.Views;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class ViewsBackend: ServiceBase
{
    private readonly GenWorkPlus<WorkViews> _workViews;
    private readonly GenWorkBasic<WorkViewsMod> _workViewsMod;
    private readonly LazySvc<IConvertToEavLight> _convertToEavLight;
    private readonly Generator<ImpExpHelpers> _impExpHelpers;
    private readonly ISite _site;
    private readonly IUser _user;

    public ViewsBackend(
        GenWorkBasic<WorkViewsMod> workViewsMod,
        GenWorkPlus<WorkViews> workViews,
        IContextOfSite context,
        LazySvc<IConvertToEavLight> convertToEavLight,
        Generator<ImpExpHelpers> impExpHelpers
    ) : base("Bck.Views")
    {
        ConnectServices(
            _workViewsMod = workViewsMod,
            _convertToEavLight = convertToEavLight,
            _impExpHelpers = impExpHelpers,
            _workViews = workViews,

            _site = context.Site,
            _user = context.User
        );
    }

    public IEnumerable<ViewDetailsDto> GetAll(int appId)
    {
        var l = Log.Fn<IEnumerable<ViewDetailsDto>>($"get all a#{appId}");

        var appViews = _workViews.New(appId);
        var contentTypes = appViews.AppWorkCtx.AppState.ContentTypes.OfScope(Scopes.Default).ToList();

        var viewList = appViews.GetAll().ToList();
        Log.A($"attribute list count:{contentTypes.Count}, template count:{viewList.Count}");
        var ser = _convertToEavLight.Value as ConvertToEavLight;
        var views = viewList.Select(view => new ViewDetailsDto
        {
            Id = view.Id, Name = view.Name, ContentType = TypeSpecs(contentTypes, view.ContentType, view.ContentItem),
            PresentationType = TypeSpecs(contentTypes, view.PresentationType, view.PresentationItem),
            ListContentType = TypeSpecs(contentTypes, view.HeaderType, view.HeaderItem),
            ListPresentationType = TypeSpecs(contentTypes, view.HeaderPresentationType, view.HeaderPresentationItem),
            TemplatePath = view.Path,
            IsHidden = view.IsHidden,
            ViewNameInUrl = view.UrlIdentifier,
            Guid = view.Guid,
            List = view.UseForList,
            HasQuery = view.QueryRaw != null,
            Used = view.Entity.Parents().Count,
            IsShared = view.IsShared,
            EditInfo = new(view.Entity),
            Metadata = ser?.CreateListOfSubEntities(view.Metadata, SubEntitySerialization.AllTrue()),
            Permissions = new() {Count = view.Entity.Metadata.Permissions.Count()},
        }).ToList();
        return l.Return(views, $"{views.Count}");
    }


    /// <summary>
    /// Helper to prepare a quick-info about 1 content type
    /// </summary>
    /// <param name="allCTs"></param>
    /// <param name="staticName"></param>
    /// <param name="maybeEntity"></param>
    /// <returns></returns>
    private static ViewContentTypeDto TypeSpecs(IEnumerable<IContentType> allCTs, string staticName, IEntity maybeEntity)
    {
        var found = allCTs.FirstOrDefault(ct => ct.NameId == staticName);
        return new()
        {
            StaticName = staticName, Id = found?.Id ?? 0, Name = found == null ? "no content type" : found.Name,
            DemoId = maybeEntity?.EntityId ?? 0,
            DemoTitle = maybeEntity?.GetBestTitle() ?? ""
        };
    }

    public bool Delete(int appId, int id)
    {
        // todo: extra security to only allow zone change if host user
        Log.A($"delete a{appId}, t:{id}");
        var app = _impExpHelpers.New().GetAppAndCheckZoneSwitchPermissions(_site.ZoneId, appId, _user, _site.ZoneId);
        _workViewsMod.New(app).DeleteView(id);
        return true;
    }
}