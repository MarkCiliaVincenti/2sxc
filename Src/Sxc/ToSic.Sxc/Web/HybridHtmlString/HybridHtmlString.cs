﻿using ToSic.Eav.Documentation;
using ToSic.Eav.Plumbing;

namespace ToSic.Sxc.Web
{
    /// <inheritdoc />
    [InternalApi_DoNotUse_MayChangeWithoutNotice("Helper to ensure that code providing an IHtmlString will work on .net Framework and .net Standard")]
    public class HybridHtmlString: IHybridHtmlString, IString
    {
        public HybridHtmlString(string value)
        {
            _value = value;
        }

        private string _value { get; }

        /// <summary>
        /// Standard ToString overload - used when concatenating strings.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => _value;

#if NETSTANDARD
        public void WriteTo(System.IO.TextWriter writer, System.Text.Encodings.Web.HtmlEncoder encoder) 
            => new Microsoft.AspNetCore.Html.HtmlString(ToString()).WriteTo(writer, encoder);
#endif

#if NETFRAMEWORK
        public string ToHtmlString() => ToString();
#endif
    }
}