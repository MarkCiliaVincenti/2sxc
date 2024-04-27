﻿using System;

namespace ToSic.Sxc.Oqt.Shared.Helpers;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
public class ErrorHelper
{
    public static string ErrorMessage(Exception ex, bool isSupreUser = false)
    {
        string errorMessage = ex.Message;

        if (isSupreUser)
        {
            errorMessage += " - " + ex.StackTrace;
            if (ex.InnerException != null)
            {
                errorMessage += " - " + ex.InnerException.Message;
                errorMessage += " - " + ex.InnerException.StackTrace;
            }
        }

        return errorMessage;
    }
}