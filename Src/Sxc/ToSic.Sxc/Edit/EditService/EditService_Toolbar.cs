﻿using ToSic.Razor.Markup;
using ToSic.Sxc.Edit.Toolbar;
using IEntity = ToSic.Eav.Data.IEntity;

namespace ToSic.Sxc.Edit.EditService;

partial class EditService
{
    private readonly string innerContentAttribute = "data-list-context";

    /// <inheritdoc />
    public IRawHtmlString Toolbar(
        object target = null,
        NoParamOrder noParamOrder = default,
        string actions = null,
        string contentType = null,
        object condition = null,
        object prefill = null,
        object settings = null,
        object toolbar = null)
        => ToolbarInternal(false, target, noParamOrder, actions, contentType, condition, prefill, settings, toolbar);

    /// <inheritdoc/>
    public IRawHtmlString TagToolbar(
        object target = null,
        NoParamOrder noParamOrder = default,
        string actions = null,
        string contentType = null,
        object condition = null,
        object prefill = null,
        object settings = null,
        object toolbar = null)
        => ToolbarInternal(true, target, noParamOrder, actions, contentType, condition, prefill, settings, toolbar);

    private IRawHtmlString ToolbarInternal(
        bool inTag,
        object target,
        NoParamOrder noParamOrder,
        string actions,
        string contentType,
        object condition,
        object prefill,
        object settings,
        object toolbar)
    {
        var l = Log.Fn<IRawHtmlString>($"enabled:{Enabled}; inline{inTag}");
        if (!Enabled) return l.ReturnNull("not enabled");
        if (!IsConditionOk(condition)) return l.ReturnNull("condition false");

        // New in v13: The first parameter can also be a ToolbarBuilder, in which case all other params are ignored
        ItemToolbarBase itmToolbar;
        if (target is IToolbarBuilder tlbBuilder)
        {
            l.A("Using new modern Item-Toolbar, will ignore all other parameters.");
            itmToolbar = new ItemToolbarV14(null, tlbBuilder);
        }
        else
        {
            // ensure that internally we always process it as an entity
            var eTarget = target as IEntity ?? (target as ICanBeEntity)?.Entity;
            if (target != null && eTarget == null)
                l.W("Creating toolbar - it seems the object provided was neither null, IEntity nor DynamicEntity");
            if (toolbar is IToolbarBuilder tlbBuilder2)
            {
                l.A("Using new modern Item-Toolbar with an entity, will ignore all other parameters.");
                itmToolbar = new ItemToolbarV14(eTarget, tlbBuilder2);
            }
            else
            {
                l.A("Using classic mode, with all parameters.");
                itmToolbar = ItemToolbarPicker.ItemToolbar(eTarget, actions, contentType,
                    prefill: prefill, settings: settings, toolbar: toolbar);
            }
        }

        var result = inTag
            ? new(itmToolbar.ToolbarAsAttributes())
            : new RawHtmlString(itmToolbar.ToolbarAsTag);
        return l.Return(result, "ok");
    }

    private bool IsConditionOk(object condition) => Log.Func(() =>
    {
        // Null = no condition and certainly not false, say ok
        if (condition == null) return (true, "null,true");

        // Bool (non-null) and nullable
        if (condition is bool b && b == false) return (false, "bool false");
        if (condition as bool? == false) return (false, "null false");

        // Int are only false if exactly 0
        if (condition is int i && i == 0) return (false, "int 0");
        if (condition as int? == 0) return (false, "int nullable 0");

        // String
        if (condition is string s &&
            string.Equals(s, false.ToString(), StringComparison.InvariantCultureIgnoreCase))
            return (false, "string false");

        // Anything else: true
        return (true, "default,true");
    });

}