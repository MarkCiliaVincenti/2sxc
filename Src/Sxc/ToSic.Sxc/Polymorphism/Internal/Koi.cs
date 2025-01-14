﻿using Connect.Koi;
using static System.StringComparison;

namespace ToSic.Sxc.Polymorphism.Internal;

[PolymorphResolver("Koi")]
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class Koi(ICss pageCss) : IResolver
{
    public string Name => "Koi";

    public const string ModeCssFramework= "cssFramework";

    public string Edition(string parameters, ILog log) => log.Func(() =>
    {
        if (!string.Equals(parameters, ModeCssFramework, InvariantCultureIgnoreCase))
            return (null, "unknown param");
        // Note: this is still using the global object which we want to get rid of
        // But to use DI, we must refactor Polymorphism
        var cssFramework = pageCss.Framework; // Connect.Koi.Koi.Css;
        return (cssFramework, cssFramework);
    });
}