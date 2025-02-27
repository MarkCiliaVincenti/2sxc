﻿using System.Collections;
using ToSic.Eav.Apps;
using ToSic.Lib.DI;
using ToSic.Lib.Services;
using ToSic.Razor.Markup;
using ToSic.Sxc.Code.Internal;

namespace ToSic.Sxc.Edit.Toolbar.Internal;

/// <summary>
/// INTERNAL: Toolbar Builder implementation.
/// </summary>
/// <remarks>
/// We cannot make this internal :(
/// Reason is that as soon as we run something like `Kit.Toolbar.Empty(Content)` in a razor file,
/// the result is dynamic - so the compiler evaluates the final object at runtime.
/// If the ToolbarBuilder is internal, things start to fail.
/// eg. AsTag() will fail, saying that RawHtmlString doesn't have that
/// So for now :( it must remain public.
/// </remarks>
[System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
public partial class ToolbarBuilder: RawHtmlString, IEnumerable<string>, IToolbarBuilder, INeedsCodeApiService
{

    #region Constructors and Init

    public class MyServices: MyServicesBase
    {
        public MyServices(
            LazySvc<IAppStates> appStatesLazy,
            LazySvc<ToolbarButtonDecoratorHelper> toolbarButtonHelper
        )
        {
            ConnectServices(
                ToolbarButtonHelper = toolbarButtonHelper,
                AppStatesLazy = appStatesLazy
            );
        }

        internal readonly LazySvc<IAppStates> AppStatesLazy;
        internal LazySvc<ToolbarButtonDecoratorHelper> ToolbarButtonHelper { get; }
    }

    /// <summary>
    /// Public constructor for DI
    /// </summary>
    /// <param name="services"></param>
    public ToolbarBuilder(MyServices services) => Services = services.ConnectServices(Log);
    protected readonly MyServices Services;

    /// <summary>
    /// Clone-constructor
    /// </summary>
    internal ToolbarBuilder(ToolbarBuilder parent, IEnumerable<ToolbarRuleBase> replaceRules = null) : this(parent.Services)
    {
        this.LinkLog(parent.Log);
        _currentAppIdentity = parent._currentAppIdentity;
        _DynCodeRoot = parent._DynCodeRoot;
        _configuration = parent._configuration;
        _utils = parent._utils;
        Rules.AddRange(replaceRules ?? parent.Rules);
    }

    public ILog Log { get; } = new Log(SxcLogName + ".TlbBld");

    private IAppIdentity _currentAppIdentity;

    public void ConnectToRoot(ICodeApiService codeRoot)
    {
        if (codeRoot == null) return;
        _DynCodeRoot = codeRoot;
        _currentAppIdentity = codeRoot.App;
        Services.ToolbarButtonHelper.Value.MainAppIdentity = _currentAppIdentity;
    }
    private ICodeApiService _DynCodeRoot;

    #endregion

    private ToolbarBuilderConfiguration _configuration;

    private ToolbarBuilderUtilities Utils => _utils ??= new();
    private ToolbarBuilderUtilities _utils;

    internal List<ToolbarRuleBase> Rules { get; } = [];

    public IToolbarBuilder Toolbar(
        string toolbarTemplate,
        object target = default,
        NoParamOrder noParamOrder = default,
        Func<ITweakButton, ITweakButton> tweak = default,
        object ui = default,
        object parameters = default,
        object prefill = default
        //ICanBeEntity root = default,
        //bool? autoDemoMode = default
    )
    {
        var updated = this.AddInternal(new ToolbarRuleToolbar(toolbarTemplate, ui: PrepareUi(ui)));
        if (new[] { target, parameters, prefill, tweak }.Any(x => x != null))
            updated = updated.Parameters(target, tweak: tweak, parameters: parameters, prefill: prefill);

        //if (root != default || autoDemoMode != default)
        //    updated = ((ToolbarBuilder)updated).With(root: root, autoDemoMode: autoDemoMode);
        return updated;
    }


    private T FindRule<T>() where T : class => Rules.FirstOrDefault(r => r is T) as T;


    #region Enumerators

    [PrivateApi]
    public IEnumerator<string> GetEnumerator()
    {
        var rulesToDeliver = Rules;

        // **Special**
        // Previously standalone toolbars also hovered based on their wrapper DIV.
        // But this isn't actually useful any more - normally hover is done with a non-standalone toolbar.
        // But we cannot change the JS defaults, because that would affect old toolbars
        // So any standalone toolbar created using the tag-builder will automatically add a settings
        // to not-hover by default. 
        // The rule must be added to the top of the list, so that any other settings will take precedence,
        // Including UI rules added to the toolbar itself
        if (_configuration?.Mode == ToolbarHtmlModes.Standalone)
        {
            var standaloneSettings = new ToolbarRuleSettings(show: "always", hover: "none");
            rulesToDeliver = new List<ToolbarRule> { standaloneSettings }.Concat(Rules).ToList();
        }

        return rulesToDeliver.Select(r => r.ToString()).GetEnumerator();
    }

    [PrivateApi]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

}