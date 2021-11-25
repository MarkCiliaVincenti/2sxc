﻿using ToSic.Sxc.Context;

namespace ToSic.Sxc.Apps.Assets
{
    public partial class AssetTemplates
    {
        public const string CsCodeTemplateName = "PleaseRenameClass";

        /// <summary>
        /// This only works for Dnn, Hybrid doesn't have this concept as we can't access functions from outside
        /// </summary>
        public static readonly TemplateInfo DnnCsCode =
            new TemplateInfo(TemplateKey.CsHtmlCode, "Razor Code (Dnn only)", Extension.CodeCshtml, ForTemplate, "DetailsTemplate")
            {
                Body = @"@inherits Custom.Dnn.Razor12
@* This inherits statement gets you features like App, CmsContext, Data as well as Dnn etc. - you can delete this comment *@
@using ToSic.Razor.Blade;

@functions {
  public string Hello() {
    return ""Hello from inner code"";
  }

  dynamic MessageHelper(string message) {
    return Tag.Div(message + ""!"");
  }
}
",
                Description = "razor page c# code hybrid template",
                PlatformTypes = PlatformType.Dnn,
                Prefix = "_",
            };




        public static readonly TemplateInfo DnnSearch =
            new TemplateInfo(TemplateKey.CustomSearchCsCode, "Dnn Search Integration (c#)", Extension.Cs, ForSearch, "DnnSearch")
            {
                Body = @"using System;
using System.Collections.Generic;
using System.Linq;
using ToSic.Sxc.Context;
using ToSic.Sxc.Search;
/*
Custom code which views use to customize how dnn search treats data of that view.
It's meant for customizing the internal indexer of the platform, _not_ for Google Search.

To use it, create a custom code (.cs) file which implements ICustomizeSearch interface.
You can also inherit from a DynamicCode base class (like Code12) if you need more functionality.

For more guidance on search customizations, see https://r.2sxc.org/customize-search
*/
public class " + CsCodeTemplateName + @" : Custom.Hybrid.Code12, ICustomizeSearch
{
    /// <summary>
    /// Populate the search
    /// </summary>
    /// <param name=""searchInfos"">Dictionary containing the streams and items in the stream for this search.</param>
    /// <param name=""moduleInfo"">Module information with which you can find out what page it's on etc.</param>
    /// <param name=""beginDate"">Last time the indexer ran - because the data you will get is only what was modified since.</param>
    public void CustomizeSearch(Dictionary<string, List<ISearchItem>> searchInfos, IModule moduleInfo, DateTime beginDate)
    {
        // Set this to true if you want to see logs of this search in the insights
        // Only do this while developing, otherwise you'll flood the logs and never see the important parts
        Log.Preserve = false;
        
        foreach (var si in searchInfos[""AllPosts""])
        {
            var entity = AsDynamic(si.Entity);
            si.Title = ""Title: "" + entity.Title;
            si.QueryString = ""details="" + entity.UrlKey;
        }

        // Remove not needed streams
        var keys = searchInfos.Keys.ToList();
        foreach (var key in keys)
        {
            if (key != ""AllPosts"")
            {
                searchInfos.Remove(key);
            }
        }
    }
}
",
                Description =
                    "custom search c# code to customize how dnn search treats data of view, see https://r.2sxc.org/customize-search",
                PlatformTypes = PlatformType.Dnn,
            };

        
    }
}