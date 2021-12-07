﻿using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using ToSic.Sxc.DotNet;

namespace ToSic.Sxc.Dnn.Web
{
    /// <summary>
    /// Special class to provide HttpRequest parameters in DNN
    ///
    /// It needs this, because DNN automatically creates invisible parameters like tabid=323 and language=en-US
    /// But if we want to create a link to the same page again, then this would result in wrong paths. 
    /// </summary>
    public class DnnHttp: HttpNetFramework
    {
        public override NameValueCollection QueryStringParams
        {
            get
            {
                if (_queryStringValues != null) return _queryStringValues;

                // This would be the better way, but it doesn't work, because DNN will often create paths like
                // /value/27 instead of ?value=27
                //var separator = Request.RawUrl.IndexOf('?');
                //if (separator == -1) return _queryStringValues = new NameValueCollection();
                //var queryPart = Request.RawUrl.Substring(separator);
                //var lightList = HttpUtility.ParseQueryString(queryPart);

                var rewrapped = new NameValueCollection(base.QueryStringParams);
                return _queryStringValues = FilterOutDnnParams(rewrapped);
            }
        }

        private NameValueCollection _queryStringValues;

        private NameValueCollection FilterOutDnnParams(NameValueCollection original)
        {
            const string tabId = "TabId";
            const string language = "language";

            // DNN adds these automatically, but does it with exactly this spelling, so that's the only one we'll catch
            original.Remove(tabId);
            original.Remove(language);
            return original;
        }

    }
}