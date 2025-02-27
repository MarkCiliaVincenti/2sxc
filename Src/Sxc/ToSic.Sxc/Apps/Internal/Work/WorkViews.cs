﻿using ToSic.Eav.Apps;
using ToSic.Eav.Apps.Internal;
using ToSic.Eav.Apps.Internal.Ui;
using ToSic.Eav.Apps.Internal.Work;
using ToSic.Eav.Context;
using ToSic.Eav.DataFormats.EavLight;
using ToSic.Eav.DataSource.Internal.Query;
using ToSic.Eav.Internal.Environment;
using ToSic.Eav.Metadata;
using ToSic.Lib.DI;
using ToSic.Lib.Helpers;
using ToSic.Sxc.Blocks;
using ToSic.Sxc.Blocks.Internal;

// note: not sure if the final namespace should be Sxc.Apps or Sxc.Views
namespace ToSic.Sxc.Apps.Internal.Work;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class WorkViews: WorkUnitBase<IAppWorkCtxPlus>
{

    #region Constructor / DI

    private readonly GenWorkPlus<WorkEntities> _appEntities;
    private readonly LazySvc<QueryDefinitionBuilder> _qDefBuilder;
    private readonly LazySvc<IValueConverter> _valConverterLazy;
    private readonly IZoneCultureResolver _cultureResolver;
    private readonly IConvertToEavLight _dataToFormatLight;
    private readonly LazySvc<AppIconHelpers> _appIconHelpers;

    public WorkViews(
        GenWorkPlus<WorkEntities> appEntities,
        LazySvc<IValueConverter> valConverterLazy,
        IZoneCultureResolver cultureResolver,
        IConvertToEavLight dataToFormatLight,
        LazySvc<AppIconHelpers> appIconHelpers,
        LazySvc<QueryDefinitionBuilder> qDefBuilder) : base("Cms.ViewRd")
    {
        ConnectServices(
            _appEntities = appEntities,
            _valConverterLazy = valConverterLazy,
            _cultureResolver = cultureResolver,
            _dataToFormatLight = dataToFormatLight,
            _appIconHelpers = appIconHelpers,
            _qDefBuilder = qDefBuilder
        );
    }

    #endregion

    private List<IEntity> ViewEntities => _viewDs.Get(() => _appEntities.New(AppWorkCtx).Get(AppConstants.TemplateContentType).ToList());
    private readonly GetOnce<List<IEntity>> _viewDs = new();

    public IList<IView> GetAll()
        => _all ??= ViewEntities.Select(p => ViewOfEntity(p, "")).OrderBy(p => p.Name).ToList();
    private IList<IView> _all;

    public IEnumerable<IView> GetRazor() => GetAll().Where(t => t.IsRazor);
    public IEnumerable<IView> GetToken() => GetAll().Where(t => !t.IsRazor);


    public IView Get(int templateId) => ViewOfEntity(ViewEntities.One(templateId), templateId);

    public IView Get(Guid guid) => ViewOfEntity(ViewEntities.One(guid), guid);

    private IView ViewOfEntity(IEntity templateEntity, object templateId) =>
        templateEntity == null
            ? throw new("The template with id '" + templateId + "' does not exist.")
            : new View(templateEntity, [_cultureResolver.CurrentCultureCode], Log, _qDefBuilder);


    internal IEnumerable<TemplateUiInfo> GetCompatibleViews(IApp app, BlockConfiguration blockConfiguration)
    {
        IEnumerable<IView> availableTemplates;
        var items = blockConfiguration.Content;

        // if any items were already initialized...
        if (items.Any(e => e != null))
            availableTemplates = GetFullyCompatibleViews(blockConfiguration);

        // if it's only nulls, and only one (no list yet)
        else if (items.Count <= 1)
            availableTemplates = GetAll(); 

        // if it's a list of nulls, only allow lists
        else
            availableTemplates = GetAll().Where(p => p.UseForList);

        var thumbnailHelper = _appIconHelpers.Value;

        var result = availableTemplates.Select(t => new TemplateUiInfo
        {
            TemplateId = t.Id,
            Name = t.Name,
            ContentTypeStaticName = t.ContentType,
            IsHidden = t.IsHidden,
            Thumbnail = thumbnailHelper.IconPathOrNull(app, t, PathTypes.Link),
            IsDefault = t.Metadata.HasType(Decorators.IsDefaultDecorator),
        });
        return result;
    }


    /// <summary>
    /// Get templates which match the signature of possible content-items, presentation etc. of the current template
    /// </summary>
    /// <param name="blockConfiguration"></param>
    /// <returns></returns>
    private IEnumerable<IView> GetFullyCompatibleViews(BlockConfiguration blockConfiguration)
    {
        var isList = blockConfiguration.Content.Count > 1;

        var compatibleTemplates = GetAll().Where(t => t.UseForList || !isList);
        compatibleTemplates = compatibleTemplates
            .Where(t => blockConfiguration.Content.All(c => c == null) || blockConfiguration.Content.First(e => e != null).Type.NameId == t.ContentType)
            .Where(t => blockConfiguration.Presentation.All(c => c == null) || blockConfiguration.Presentation.First(e => e != null).Type.NameId == t.PresentationType)
            .Where(t => blockConfiguration.Header.All(c => c == null) || blockConfiguration.Header.First(e => e != null).Type.NameId == t.HeaderType)
            .Where(t => blockConfiguration.HeaderPresentation.All(c => c == null) || blockConfiguration.HeaderPresentation.First(e => e != null).Type.NameId == t.HeaderPresentationType);

        return compatibleTemplates;
    }


    // todo: check if this call could be replaced with the normal ContentTypeController.Get to prevent redundant code
    public IEnumerable<ContentTypeUiInfo> GetContentTypesWithStatus(string appPath, string appPathShared)
    {
        var templates = GetAll().ToList();
        var visible = templates.Where(t => !t.IsHidden).ToList();

        var valConverter = _valConverterLazy.Value;

        return AppWorkCtx.AppState.ContentTypes.OfScope(Scopes.Default) 
            .Where(ct => templates.Any(t => t.ContentType == ct.NameId)) // must exist in at least 1 template
            .OrderBy(ct => ct.Name)
            .Select(ct =>
            {
                var details = ct.Metadata.DetailsOrNull;
                var thumbnail = valConverter.ToValue(details?.Icon);
                if (AppIconHelpers.HasAppPathToken(thumbnail))
                    thumbnail = AppIconHelpers.AppPathTokenReplace(thumbnail, appPath, appPathShared);
                return new ContentTypeUiInfo {
                    StaticName = ct.NameId,
                    Name = ct.Name,
                    IsHidden = visible.All(t => t.ContentType != ct.NameId),   // must check if *any* template is visible, otherwise tell the UI that it's hidden
                    Thumbnail = thumbnail,
                    Properties = _dataToFormatLight.Convert(details?.Entity),
                    IsDefault = ct.Metadata.HasType(Decorators.IsDefaultDecorator),
                };
            });
    }



}