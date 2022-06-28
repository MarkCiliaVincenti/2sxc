﻿using System;
using System.Collections.Generic;
using ToSic.Eav.Plumbing;
using static ToSic.Sxc.Edit.Toolbar.ToolbarRuleOperations;

namespace ToSic.Sxc.Edit.Toolbar
{
    internal enum ToolbarRuleOperations
    {
        BtnAdd = '+',
        BtnAddAuto = '±',
        BtnModify = '%',
        BtnRemove = '-',
        BtnUnknown = '¿',
    }

    internal class ToolbarRuleOps
    {

        internal static Dictionary<string, ToolbarRuleOperations> ToolbarRuleOpSynonyms =
            new Dictionary<string, ToolbarRuleOperations>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "mod", BtnModify },
                { "modify", BtnModify },
                { "add", BtnAdd },
                { "addauto", BtnAddAuto },
                { "rem", BtnRemove},
                { "remove", BtnRemove },
            };

        internal static char FindInFlags(string flags, ToolbarRuleOperations defOp)
        {
            if (!flags.HasValue()) return (char)defOp;

            var parts = flags.Split(',');
            foreach (var f in parts)
            {
                var maybeOp = FindOperation(f.Trim(), BtnUnknown);
                if (maybeOp != (char)BtnUnknown) return maybeOp;
            }

            return (char)defOp;
        }

        private static char FindOperation(string op, ToolbarRuleOperations defOp)
        {
            if (!op.HasValue()) return (char)defOp;

            if (op.Length == 1 && Enum.TryParse(op, true, out ToolbarRuleOperations foundOp))
                return (char)foundOp;

            if (ToolbarRuleOpSynonyms.TryGetValue(op, out var foundSyn))
                return (char)foundSyn;

            return (char)defOp;
        }

    }
}