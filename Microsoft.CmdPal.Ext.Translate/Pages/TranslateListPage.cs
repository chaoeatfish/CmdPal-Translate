// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CmdPal.Ext.Translate.Commands;
using Microsoft.CmdPal.Ext.Translate.Core;
using Microsoft.CmdPal.Ext.Translate.Helpers;
using Microsoft.CmdPal.Ext.Translate.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Translate.Pages;

/// <summary>
/// Mirrors the Everything extension's organisation: one top-level command opens a live list page;
/// results are ListItems with title, subtitle, icon and a bottom-right command bar.
/// </summary>
internal sealed partial class TranslateListPage : DynamicListPage
{
    private readonly TranslationService _service;
    private readonly SettingsManager _settings;
    private readonly ICommand _settingsPage;
    private readonly object _gate = new();

    private IListItem[] _items;

    public TranslateListPage(TranslationService service, SettingsManager settings, ICommand settingsPage)
    {
        _service = service;
        _settings = settings;
        _settingsPage = settingsPage;

        Icon = Icons.Translate;
        Title = Resources.ExtensionName;
        Name = Resources.Open;
        PlaceholderText = Resources.PlaceholderText;

        _service.OutcomeChanged += OnOutcome;
        _items = [BuildIdleItem()];
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        var parsed = TriggerParser.ParsePageQuery(newSearch);
        if (!parsed.IsMatch || string.IsNullOrWhiteSpace(parsed.Text))
        {
            _service.Cancel();
            lock (_gate)
            {
                _items = [BuildIdleItem()];
            }

            RaiseItemsChanged();
            return;
        }

        var (from, to) = LanguageDetector.Resolve(
            parsed.Text,
            _settings.SourceLanguage,
            _settings.TargetLanguage,
            parsed.TargetOverride);
        _service.Request(parsed.Text, from, to);
    }

    public override IListItem[] GetItems()
    {
        lock (_gate)
        {
            return _items;
        }
    }

    private void OnOutcome(object? sender, TranslationOutcome outcome)
    {
        if (outcome.State == TranslationState.Failed)
        {
            ExtensionLog.Error("Translation failed: " + outcome.Error);
        }

        lock (_gate)
        {
            _items = BuildItems(outcome);
        }

        RaiseItemsChanged();
    }

    private IListItem[] BuildItems(TranslationOutcome outcome)
    {
        if (outcome.State != TranslationState.Success || string.IsNullOrEmpty(outcome.TranslatedText))
        {
            return [BuildStatusItem(outcome)];
        }

        var engine = outcome.Engine == "cache" ? Resources.CacheHit : outcome.Engine;
        var items = new List<IListItem>
        {
            new ListItem(new CopyDynamicTextCommand(
                Resources.CopyTranslation,
                "com.microsoft.cmdpal.translate.copy.result",
                Icons.Copy,
                () => outcome.TranslatedText))
            {
                Title = outcome.TranslatedText,
                Subtitle = outcome.Direction + " · " + engine + " · " + outcome.ElapsedMilliseconds + " ms",
                Icon = Icons.Translate,
                MoreCommands = BuildCommands(outcome),
            },
        };

        if (_settings.MaxResults >= 2 && !string.IsNullOrEmpty(outcome.SourceText))
        {
            items.Add(new ListItem(new CopyDynamicTextCommand(
                Resources.CopySource,
                "com.microsoft.cmdpal.translate.copy.source",
                Icons.Copy,
                () => outcome.SourceText))
            {
                Title = outcome.SourceText,
                Subtitle = Resources.CopySource,
                Icon = Icons.Copy,
                MoreCommands = BuildCommands(outcome),
            });
        }

        return items.ToArray();
    }

    private ListItem BuildStatusItem(TranslationOutcome outcome)
    {
        var (title, subtitle, icon) = outcome.State switch
        {
            TranslationState.NoConfiguration => (Resources.NoConfig, Resources.NoConfigSubtitle, Icons.Warning),
            TranslationState.Failed => (Resources.Failed, Resources.FailedSubtitle, Icons.Warning),
            TranslationState.Translating => (Resources.Translating, outcome.Direction, Icons.Translate),
            TranslationState.Pending => (Resources.Pending, outcome.SourceText, Icons.Translate),
            _ => (Resources.Idle, Resources.PlaceholderText, Icons.Translate),
        };

        return new ListItem(new NoOpCommand())
        {
            Title = title,
            Subtitle = subtitle,
            Icon = icon,
            MoreCommands = BuildCommands(outcome),
        };
    }

    private ListItem BuildIdleItem() => new ListItem(new NoOpCommand())
    {
        Title = Resources.Idle,
        Subtitle = Resources.PlaceholderText,
        Icon = Icons.Translate,
        MoreCommands = BuildCommands(TranslationOutcome.Idle()),
    };

    private CommandContextItem[] BuildCommands(TranslationOutcome outcome) =>
    [
        new CommandContextItem(new CopyDynamicTextCommand(
            Resources.CopyTranslation,
            "com.microsoft.cmdpal.translate.copy.command",
            Icons.Copy,
            () => outcome.TranslatedText)),
        new CommandContextItem(new SwapLanguageCommand(_service)),
        new CommandContextItem(new TranslateDetailPage(outcome)),
        new CommandContextItem(new TranslateClipboardCommand(_service, _settings)),
        new CommandContextItem(new ImportKeyCommand()),
        new CommandContextItem(_settingsPage),
    ];
}
