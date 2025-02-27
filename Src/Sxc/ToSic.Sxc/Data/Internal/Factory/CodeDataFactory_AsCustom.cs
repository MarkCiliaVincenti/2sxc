﻿using System.Collections;

namespace ToSic.Sxc.Data.Internal;

partial class CodeDataFactory
{
    /// <summary>
    /// Convert an object to a custom type, if possible.
    /// If the object is an entity-like thing, that will be converted.
    /// If it's a list of entity-like things, the first one will be converted.
    /// </summary>
    public TCustom AsCustom<TCustom>(object source, NoParamOrder protector = default, bool mock = false)
        where TCustom : class, ITypedItemWrapper16, ITypedItem, new()
        => source switch
        {
            null when !mock => null,
            TCustom alreadyT => alreadyT,
            _ => AsCustomFromItem<TCustom>(source as ITypedItem ?? AsItem(source))
        };

    internal static T AsCustomFromItem<T>(ITypedItem item) where T : class, ITypedItemWrapper16, ITypedItem, new()
    {
        if (item == null) return null;
        if (item is T t) return t;
        var newT = new T();
        newT.Setup(item);
        return newT;
    }

    ///// <summary>
    ///// WIP / experimental, would be for types which are not as T, but as a type-object.
    ///// Not in use, so not fully tested.
    /////
    ///// Inspired by https://stackoverflow.com/questions/3702916/is-there-a-typeof-inverse-operation
    ///// </summary>
    ///// <param name="t"></param>
    ///// <param name="source"></param>
    ///// <param name="protector"></param>
    ///// <param name="mock"></param>
    ///// <returns></returns>
    //public object AsCustom(Type t, ICanBeEntity source, NoParamOrder protector = default, bool mock = false)
    //{
    //    if (!mock && source == null) return null;
    //    if (source.GetType() == t) return source;

    //    var item = source as ITypedItem ?? AsItem(source);
    //    var wrapperObj = Activator.CreateInstance(t);
    //    if (wrapperObj is not ITypedItemWrapper16 wrapper) return null;
    //    wrapper.Setup(item);
    //    return wrapper;
    //}

    /// <summary>
    /// EXPERIMENTAL
    /// </summary>
    public IEnumerable<TCustom> AsCustomList<TCustom>(object source, NoParamOrder protector, bool nullIfNull)
        where TCustom : class, ITypedItemWrapper16, ITypedItem, new()
    {
        return source switch
        {
            null when nullIfNull => null,
            IEnumerable<TCustom> alreadyListT => alreadyListT,
            _ => SafeItems().Select(AsCustomFromItem<TCustom>)
        };

        IEnumerable<ITypedItem> SafeItems()
            => source switch
            {
                null => [],
                IEnumerable enumerable when !enumerable.Cast<object>().Any() => [],
                IEnumerable<ITypedItem> alreadyOk => alreadyOk,
                _ => _CodeApiSvc.Cdf.AsItems(source)
            };
    }

}