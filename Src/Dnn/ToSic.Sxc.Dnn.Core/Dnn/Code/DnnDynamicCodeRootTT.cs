﻿using ToSic.Eav.Documentation;
using ToSic.Eav.Plumbing;
using ToSic.Sxc.Code;
using ToSic.Sxc.Dnn.Run;
using ToSic.Sxc.Services;

namespace ToSic.Sxc.Dnn.Code
{
    [PrivateApi]
    public class DnnDynamicCodeRoot<TModel, TServiceKit> : DynamicCodeRoot<TModel, TServiceKit>, Sxc.Code.IDynamicCode, IDnnDynamicCode, IHasDynamicCodeRoot
        where TModel : class
        where TServiceKit : ServiceKit
    {
        public DnnDynamicCodeRoot(Dependencies dependencies): base(dependencies, DnnConstants.LogName) { }

        /// <summary>
        /// Dnn context with module, page, portal etc.
        /// </summary>
        public IDnnContext Dnn => _dnn.Get(GetService<IDnnContext>);
        private readonly ValueGetOnce<IDnnContext> _dnn = new ValueGetOnce<IDnnContext>();
    }
}