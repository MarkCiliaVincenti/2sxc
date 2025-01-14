﻿using ToSic.Razor.Blade;

namespace ToSic.Sxc.Web.PageService;

[Obsolete]
internal class WebPageServiceObsolete(Services.IPageService pageServiceImplementation) : ToSic.Sxc.Web.IPageService
{
    public string SetBase(string url = null)
    {
        return pageServiceImplementation.SetBase(url);
    }

    public string SetTitle(string value, string placeholder = null)
    {
        return pageServiceImplementation.SetTitle(value, placeholder);
    }

    public string SetDescription(string value, string placeholder = null)
    {
        return pageServiceImplementation.SetDescription(value, placeholder);
    }

    public string SetKeywords(string value, string placeholder = null)
    {
        return pageServiceImplementation.SetKeywords(value, placeholder);
    }

    public string SetHttpStatus(int statusCode, string message = null)
    {
        return pageServiceImplementation.SetHttpStatus(statusCode, message);
    }

    public string AddToHead(string tag)
    {
        return pageServiceImplementation.AddToHead(tag);
    }

    public string AddToHead(IHtmlTag tag)
    {
        return pageServiceImplementation.AddToHead(tag);
    }

    public string AddMeta(string name, string content)
    {
        return pageServiceImplementation.AddMeta(name, content);
    }

    public string AddOpenGraph(string property, string content)
    {
        return pageServiceImplementation.AddOpenGraph(property, content);
    }

    public string AddJsonLd(string jsonString)
    {
        return pageServiceImplementation.AddJsonLd(jsonString);
    }

    public string AddJsonLd(object jsonObject)
    {
        return pageServiceImplementation.AddJsonLd(jsonObject);
    }

    public string AddIcon(string path, NoParamOrder noParamOrder = default, string rel = "", int size = 0,
        string type = null)
    {
        return pageServiceImplementation.AddIcon(path, noParamOrder, rel, size, type);
    }

    public string AddIconSet(string path, NoParamOrder noParamOrder = default, object favicon = null,
        IEnumerable<string> rels = null,
        IEnumerable<int> sizes = null)
    {
        return pageServiceImplementation.AddIconSet(path, noParamOrder, favicon, rels, sizes);
    }

    public string Activate(params string[] keys)
    {
        return pageServiceImplementation.Activate(keys);
    }
}