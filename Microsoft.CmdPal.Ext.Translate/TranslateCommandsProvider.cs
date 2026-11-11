// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using Microsoft.CmdPal.Ext.Translate.Commands;
using Microsoft.CmdPal.Ext.Translate.Core;
using Microsoft.CmdPal.Ext.Translate.Helpers;
using Microsoft.CmdPal.Ext.Translate.Pages;
using Microsoft.CmdPal.Ext.Translate.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Translate;

public sealed partial class TranslateCommandsProvider : CommandProvider
{
    private readonly SettingsManager _settings = new();
    private readonly CompositeTranslationProvider _translationProvider;
    private readonly TranslationService _service;
    private readonly TranslateListPage _page;
    private readonly FallbackTranslateItem _fallback;
    private readonly ICommandItem[] _topLevelCommands;
    private readonly IFallbackCommandItem[] _fallbackCommands;

    public TranslateCommandsProvider()
    {
        // Unique per-extension id. Never reuse "PowerToys" or another extension's ids.
        Id = "com.microsoft.cmdpal.translate";
        DisplayName = Resources.ExtensionName;
        Icon = Icons.Translate;
        Settings = _settings.Settings;

        _translationProvider = new CompositeTranslationProvider(
            () => new TranslationCredentials(_settings.Endpoint, _settings.Region, SettingsManager.ApiKey),
            () => _settings.Engine);
        _service = new TranslationService(
            _translationProvider,
            new TranslationCache(TimeSpan.FromMinutes(_settings.CacheTtlMinutes)),
            TimeSpan.FromMilliseconds(_settings.DebounceMs),
            TimeSpan.FromSeconds(_settings.TimeoutSeconds));

        _page = new TranslateListPage(_service, _settings, _settings.Settings.SettingsPage);
        _fallback = new FallbackTranslateItem(_service, _settings);

        _topLevelCommands =
        [
            new CommandItem(_page)
            {
                Title = DisplayName,
                Subtitle = Resources.CommandSubtitle,
                Icon = Icons.Translate,
                MoreCommands =
                [
                    new CommandContextItem(_settings.Settings.SettingsPage),
                    new CommandContextItem(new ImportKeyCommand()),
                ],
            },
        ];

        _fallbackCommands = [_fallback];
    }

    public override ICommandItem[] TopLevelCommands() => _topLevelCommands;

    public override IFallbackCommandItem[]? FallbackCommands() => _fallbackCommands;

    public override void Dispose()
    {
        _service.Cancel();
        _translationProvider.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}
