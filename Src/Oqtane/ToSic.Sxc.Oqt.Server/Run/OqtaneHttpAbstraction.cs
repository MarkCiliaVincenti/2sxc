﻿using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ToSic.Sxc.Web;


namespace ToSic.Sxc.Oqt.Server.Run
{
    public class OqtaneHttpAbstraction: IHttp
    {
        public OqtaneHttpAbstraction(IHttpContextAccessor contextAccessor, IWebHostEnvironment hostingEnvironment
            //IUrlHelperFactory urlHelperFactory, 
            //IActionContextAccessor actionContextAccessor
            )
        {
            //Current = contextAccessor.HttpContext;
            _contextAccessor = contextAccessor;
            _hostingEnvironment = hostingEnvironment;
            //_urlHelperFactory = urlHelperFactory;
            //_actionContextAccessor = actionContextAccessor;
        }

        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IWebHostEnvironment _hostingEnvironment;
        //private readonly IUrlHelperFactory _urlHelperFactory;
        //private readonly IActionContextAccessor _actionContextAccessor;
        //private IUrlHelper UrlHelper => _urlHlp ??= _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext );
        private IUrlHelper _urlHlp;
        public HttpContext Current => _contextAccessor.HttpContext;


        #region Request and properties thereof
        public HttpRequest Request => Current?.Request;

        public NameValueCollection QueryString
        {
            get
            {
                if (_queryStringValues != null) return _queryStringValues;
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if(Request == null) return _queryStringValues = new NameValueCollection();
#if NET451
                return _queryStringValues = Request.QueryString;
#else
                var paramList = new NameValueCollection();
                Request.Query.ToList().ForEach(i => paramList.Add(i.Key, i.Value));
                return _queryStringValues = paramList;
#endif
            }
        }

        private NameValueCollection _queryStringValues;

        public List<KeyValuePair<string, string>> QueryStringKeyValuePairs()
        {
            if (_queryStringKeyValuePairs != null) return _queryStringKeyValuePairs;
            var qs = QueryString;
            _queryStringKeyValuePairs = qs?.AllKeys
                                            .Select(key => new KeyValuePair<string, string>(key, qs[key]))
                                            .ToList()
                                        ?? new List<KeyValuePair<string, string>>();
            return _queryStringKeyValuePairs;
            //return (from string key in qs select new KeyValuePair<string, string>(key, qs[key]))
            //    .ToList();
        }

        private List<KeyValuePair<string, string>> _queryStringKeyValuePairs;
        #endregion Request

        #region Paths

        public string TestMapPath()
        {
            return _hostingEnvironment.WebRootPath;
        }

        private string toWebAbsolute(string virtualPath)
        {
            virtualPath = virtualPath.TrimStart('~');
            return virtualPath;
        }

        public string ToAbsolute(string virtualPath)
        {
            return /*UrlHelper.Content*/toWebAbsolute(virtualPath);
        }
        public string Combine(string basePath, string relativePath)
        {
            return /*UrlHelper.Content*/toWebAbsolute(Path.Combine(basePath, relativePath));
        }


        #endregion
    }
}