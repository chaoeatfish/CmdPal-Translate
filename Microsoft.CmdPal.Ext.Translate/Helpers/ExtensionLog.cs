// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Translate.Helpers;

internal static class ExtensionLog
{
    /// <summary>Never log the request body, the endpoint query or the API key.</summary>
    public static void Error(string message)
    {
        try
        {
            ExtensionHost.LogMessage(new LogMessage() { Message = message });
        }
        catch (Exception)
        {
            // Logging must never break the extension host.
        }
    }
}
