﻿using ToSic.Eav;

namespace ToSic.Sxc.Edit.Toolbar
{
    public partial interface IToolbarBuilder
    {

        IToolbarBuilder TemplateEdit(
            object target = null,
            string noParamOrder = Parameters.Protector,
            object ui = null,
            object parameters = null
        );

        IToolbarBuilder QueryEdit(
            object target = null,
            string noParamOrder = Parameters.Protector,
            object ui = null,
            object parameters = null
        );

        IToolbarBuilder ViewEdit(
            object target = null,
            string noParamOrder = Parameters.Protector,
            object ui = null,
            object parameters = null
        );




    }
}