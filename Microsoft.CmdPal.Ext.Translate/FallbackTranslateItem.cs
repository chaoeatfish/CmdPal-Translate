// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using Microsoft.CmdPal.Ext.Translate.Commands;
using Microsoft.CmdPal.Ext.Translate.Core;
using Microsoft.CmdPal.Ext.Translate.Helpers;
using Microsoft.CmdPal.Ext.Translate.Properties;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Translate;

/// <summary>
/// Inline home-page result. Strictly gated by an explicit prefix: a query such as "report.pdf"
/// produces no item from this extension, so Everything's inline results and ranking stay intact.
/// </summary>
internal sealed partial class FallbackTranslateItem : FallbackCommandItem
{
    private const string ItemId = "com.microsoft.cmdpal.translate.fallback";

    private readonly TranslationService _service;
    private readonly SettingsManager _settings;

    public FallbackTranslateItem(TranslationService service, SettingsManager settings)
        : base(
            new CopyDynamicTextCommand(
                Resources.CopyTranslation,
                ItemId + ".copy",
                Icons.Copy,
                () => service.Current.TranslatedText),
            Resources.ExtensionName,
            ItemId)
    {
        _service = service;
        _settings = settings;
        Title = string.Empty;
        Subtitle = string.Empty;
        Icon = Icons.Translate;
        _service.OutcomeChanged += OnOutcome;
    }

    public override void UpdateQuery(string query)
    {
        var parsed = TriggerParser.Parse(query, _settings.Prefixes);
        if (!_settings.EnableInlineFallback || !parsed.IsMatch || string.IsNullOrWhiteSpace(parsed.Text))
        {
            Title = string.Empty;
            Subtitle = string.Empty;
            _service.Cancel();
            return;
        }

        var (from, to) = LanguageDetector.Resolve(
            parsed.Text,
            _settings.SourceLanguage,
            _settings.TargetLanguage,
            parsed.TargetOverride);
        _service.Request(parsed.Text, from, to);
    }

    private void OnOutcome(object? sender, TranslationOutcome outcome)
    {
        switch (outcome.State)
        {
            case TranslationState.Success:
                Title = outcome.TranslatedText ?? string.Empty;
                var engine = outcome.Engine == "cache" ? Resources.CacheHit : outcome.Engine;
                Subtitle = outcome.Direction + " · " + engine + " · " + outcome.ElapsedMilliseconds + " ms";
                break;
            case TranslationState.NoConfiguration:
                Title = Resources.NoConfig;
                Subtitle = Resources.NoConfigSubtitle;
                break;
            case TranslationState.Failed:
                Title = Resources.Failed;
                Subtitle = Resources.FailedSubtitle;
                break;
            case TranslationState.Translating:
                Title = Resources.Translating;
                Subtitle = outcome.Direction;
                break;
            case TranslationState.Pending:
                Title = Resources.Pending;
                Subtitle = outcome.SourceText;
                break;
            default:
                Title = string.Empty;
                Subtitle = string.Empty;
                break;
        }
    }
}
