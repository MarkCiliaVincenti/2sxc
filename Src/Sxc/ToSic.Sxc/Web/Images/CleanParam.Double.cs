﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ToSic.Sxc.Web
{
    internal static partial class CleanParam
    {

        internal static double? DoubleOrNull(object value)
        {
            if (value is null) return null;
            if (value is float floatVal) return floatVal;
            if (value is double dVal) return dVal;

            var strValue = RealStringOrNull(value);
            if (strValue == null) return null;
            if (!double.TryParse(strValue, out var doubleValue)) return null;
            return doubleValue;
        }

        internal static double? DoubleOrNullWithCalculation(object value)
        {
            // First, check if it's a string like "4:2" or "16/9"
            if (value is string strValue && !string.IsNullOrWhiteSpace(strValue))
            {
                // check for separator
                var separator = strValue.IndexOfAny(new[] { ':', '/' });
                if (separator > 0) // if it starts with the separator, something is wrong
                {
                    var leftPart = strValue.Substring(0, separator);
                    var rightPart = strValue.Substring(separator + 1);

                    if (double.TryParse(leftPart, out var top) && double.TryParse(rightPart, out var bottom) &&
                        top > 0 && bottom > 0)
                    {
                        var result = top / bottom;
                        return result;
                    }
                }
            }

            return DoubleOrNull(value);
        }

        //internal static bool FNearZero(float f) => Math.Abs(f) <= 0.01;
        internal static bool DNearZero(double f) => Math.Abs(f) <= 0.01;
    }
}