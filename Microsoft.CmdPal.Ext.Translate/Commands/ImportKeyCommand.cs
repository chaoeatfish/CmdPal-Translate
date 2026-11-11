// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using Microsoft.CmdPal.Ext.Translate.Helpers;
using Microsoft.CmdPal.Ext.Translate.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Translate.Commands;

/// <summary>
/// Copies the API key from the clipboard into a DPAPI-protected file. The key never appears in
/// settings.json, in the settings UI, or in logs.
/// </summary>
internal sealed partial class ImportKeyCommand : InvokableCommand
{
    public ImportKeyCommand()
    {
        Id = "com.microsoft.cmdpal.translate.importkey";
    }

    public override string Name => Resources.ImportKey;

    public override IconInfo Icon => Icons.Settings;

    public override CommandResult Invoke()
    {
        var key = ClipboardText.TryRead();
        if (string.IsNullOrWhiteSpace(key))
        {
            return CommandResult.KeepOpen();
        }

        try
        {
            return SecretStore.SaveApiKey(key)
                ? CommandResult.ShowToast(Resources.KeySaved)
                : CommandResult.KeepOpen();
        }
        catch (Exception ex)
        {
            ExtensionLog.Error("ImportKeyCommand failed: " + ex.GetType().Name);
            return CommandResult.ShowToast(Resources.KeyImportFailed);
        }
    }
}
