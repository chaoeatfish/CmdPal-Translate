// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using Microsoft.CmdPal.Ext.Translate.Helpers;
using Microsoft.CmdPal.Ext.Translate.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Translate.Commands;

/// <summary>
/// Copies text that is resolved at invocation time. The fallback item can therefore keep one
/// command instance while the translation result changes asynchronously.
/// </summary>
internal sealed partial class CopyDynamicTextCommand : InvokableCommand
{
    private readonly Func<string?> _textProvider;
    private readonly string _name;
    private readonly IconInfo _icon;

    public CopyDynamicTextCommand(string name, string id, IconInfo icon, Func<string?> textProvider)
    {
        _name = name;
        _icon = icon;
        _textProvider = textProvider;
        Id = id;
    }

    public override string Name => _name;

    public override IconInfo Icon => _icon;

    public override CommandResult Invoke()
    {
        var text = _textProvider();
        if (string.IsNullOrEmpty(text))
        {
            return CommandResult.KeepOpen();
        }

        return ClipboardText.TryWrite(text)
            ? CommandResult.ShowToast(Resources.Copied)
            : CommandResult.KeepOpen();
    }
}
