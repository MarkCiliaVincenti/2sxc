﻿using System.Text.Json;
using ToSic.Lib.DI;
using ToSic.Lib.Services;
using ToSic.Sxc.Data;
using ToSic.Sxc.Data.Internal.Wrapper;
using static ToSic.Eav.Serialization.JsonOptions;

namespace ToSic.Sxc.Services;

[PrivateApi("Hide implementation")]
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal class JsonService: ServiceBase, IJsonService
{
    private readonly Generator<CodeJsonWrapper> _wrapJsonGenerator;

    public JsonService(Generator<CodeJsonWrapper> wrapJsonGenerator) : base("Sxc.JsnSvc")
    {
        ConnectServices(
            _wrapJsonGenerator = wrapJsonGenerator
        );
    }

    /// <inheritdoc />
    public T To<T>(string json) 
        => JsonSerializer.Deserialize<T>(json, SafeJsonForHtmlAttributes);

    /// <inheritdoc />
    public object ToObject(string json)
        => JsonSerializer.Deserialize<object>(json, SafeJsonForHtmlAttributes);

    /// <inheritdoc />
    public string ToJson(object item)
        => JsonSerializer.Serialize(item, SafeJsonForHtmlAttributes);

    /// <inheritdoc />
    public string ToJson(object item, int indentation)
        => JsonSerializer.Serialize(item, SafeJsonForHtmlAttributes);

    /// <inheritdoc />
    public ITyped ToTyped(string json, NoParamOrder noParamOrder = default, string fallback = default, bool? propsRequired = default)
        => _wrapJsonGenerator.New()
            .Setup(WrapperSettings.Typed(true, true, propsRequired: propsRequired ?? true))
            .JsonToTyped(json, noParamOrder, fallback);


    /// <inheritdoc />
    public IEnumerable<ITyped> ToTypedList(string json, NoParamOrder noParamOrder = default, string fallback = default, bool? propsRequired = default)
        => _wrapJsonGenerator.New()
            .Setup(WrapperSettings.Typed(true, true, propsRequired: propsRequired ?? true))
            .JsonToTypedList(json, noParamOrder, fallback);
}