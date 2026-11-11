// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using Microsoft.CmdPal.Ext.Translate.Core;
using Microsoft.CmdPal.Ext.Translate.Helpers;
using Microsoft.CmdPal.Ext.Translate.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Translate.Commands;

internal sealed partial class SwapLanguageCommand : InvokableCommand
{
    private readonly TranslationService _service;

    public SwapLanguageCommand(TranslationService service)
    {
        _service = service;
        Id = "com.microsoft.cmdpal.translate.swap";
    }

    public override string Name => Resources.Swap;

    public override IconInfo Icon => Icons.Swap;

    public override CommandResult Invoke()
    {
        _service.Swap();
        return CommandResult.KeepOpen();
    }
}
