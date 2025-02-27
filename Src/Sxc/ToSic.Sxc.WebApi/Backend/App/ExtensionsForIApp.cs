﻿using System.Text.RegularExpressions;
using ToSic.Eav.Apps.Specs;

namespace ToSic.Sxc.Backend.App;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public static class ExtensionsForIApp
{
    //public static string VersionSafe(this IApp app) => app.Configuration.Version?.ToString() ?? "";

    public static string VersionSafe(this IAppSpecs app) => app.Configuration.Version?.ToString() ?? "";

    public static string NameWithoutSpecialChars(this IAppSpecs app) => Regex.Replace(app.Name, "[^a-zA-Z0-9-_]", "");

}