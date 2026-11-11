// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using Microsoft.CmdPal.Ext.Translate.Core;
using Microsoft.CmdPal.Ext.Translate.Helpers;
using Microsoft.CmdPal.Ext.Translate.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Translate.Commands;

/// <summary>Explicit user action. Reads the clipboard once, then refreshes the shared list page.</summary>
internal sealed partial class TranslateClipboardCommand : InvokableCommand
{
    private readonly TranslationService _service;
    private readonly SettingsManager _settings;

    public TranslateClipboardCommand(TranslationService service, SettingsManager settings)
    {
        _service = service;
        _settings = settings;
        Id = "com.microsoft.cmdpal.translate.clipboard";
    }

    public override string Name => Resources.TranslateClipboard;

    public override IconInfo Icon => Icons.Clipboard;

    public override CommandResult Invoke()
    {
        var text = ClipboardText.TryRead();
        if (string.IsNullOrWhiteSpace(text))
        {
            return CommandResult.KeepOpen();
        }

        var (from, to) = LanguageDetector.Resolve(
            text,
            _settings.SourceLanguage,
            _settings.TargetLanguage,
            null);
        _service.Request(text, from, to);
        return CommandResult.KeepOpen();
    }
}
