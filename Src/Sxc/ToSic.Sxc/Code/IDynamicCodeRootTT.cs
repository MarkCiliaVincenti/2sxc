﻿using ToSic.Sxc.Services;

namespace ToSic.Sxc.Code
{
    public interface IDynamicCodeRoot<out TModel, out TServiceKit>: IDynamicCodeRoot, IDynamicCode<TModel, TServiceKit>
        where TModel : class
        where TServiceKit : ServiceKit
    {
    }
}