// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.CmdPal.Ext.Translate.Properties;

namespace Microsoft.CmdPal.Ext.Translate.Helpers;

/// <summary>
/// Settings live in their own namespace and their own JSON file. They never share state with
/// an Everything extension (its filters.toml / settings.json are untouched).
/// </summary>
internal sealed partial class SettingsManager : JsonSettingsManager
{
    private const string NamespaceKey = "translate";
    private const string DefaultEndpoint = "https://api.cognitive.microsofttranslator.com";

    private static readonly List<ChoiceSetSetting.Choice> LanguageChoices =
    [
        new(Resources.LangAuto, "auto"),
        new(Resources.LangZh, "zh"),
        new(Resources.LangEn, "en"),
    ];

    private static readonly List<ChoiceSetSetting.Choice> EngineChoices =
    [
        new(Resources.EngineAuto, "auto"),
        new(Resources.EngineFree, "free"),
        new(Resources.EngineAzure, "azure"),
    ];

    private readonly ChoiceSetSetting _engine = new(
        N("engine"), Resources.SettingEngine, Resources.SettingEngineDesc, EngineChoices);

    private readonly ChoiceSetSetting _sourceLanguage = new(
        N("sourceLanguage"), Resources.SettingSource, Resources.SettingSourceDesc, LanguageChoices);

    private readonly ChoiceSetSetting _targetLanguage = new(
        N("targetLanguage"), Resources.SettingTarget, Resources.SettingTargetDesc, LanguageChoices);

    private readonly TextSetting _endpoint = new(
        N("endpoint"), Resources.SettingEndpoint, Resources.SettingEndpointDesc, DefaultEndpoint);

    private readonly TextSetting _region = new(
        N("region"), Resources.SettingRegion, Resources.SettingRegionDesc, string.Empty)
    { Placeholder = "e.g. chinaeast2" };

    private readonly TextSetting _prefixes = new(
        N("prefixes"), Resources.SettingPrefixes, Resources.SettingPrefixesDesc, "tr,翻译");

    private readonly ToggleSetting _enableInlineFallback = new(
        N("enableInlineFallback"), Resources.SettingInline, Resources.SettingInlineDesc, true);

    private readonly TextSetting _debounceMs = new(
        N("debounceMs"), Resources.SettingDebounce, Resources.SettingDebounceDesc, "300");

    private readonly TextSetting _timeoutSeconds = new(
        N("timeoutSeconds"), Resources.SettingTimeout, Resources.SettingTimeoutDesc, "5");

    private readonly TextSetting _cacheTtlMinutes = new(
        N("cacheTtlMinutes"), Resources.SettingCacheTtl, Resources.SettingCacheTtlDesc, "60");

    private readonly TextSetting _maxResults = new(
        N("maxResults"), Resources.SettingMaxResults, Resources.SettingMaxResultsDesc, "5");

    public SettingsManager()
    {
        FilePath = SettingsJsonPath();
        Settings.Add(_engine);
        Settings.Add(_sourceLanguage);
        Settings.Add(_targetLanguage);
        Settings.Add(_endpoint);
        Settings.Add(_region);
        Settings.Add(_prefixes);
        Settings.Add(_enableInlineFallback);
        Settings.Add(_debounceMs);
        Settings.Add(_timeoutSeconds);
        Settings.Add(_cacheTtlMinutes);
        Settings.Add(_maxResults);
        LoadSettings();
        Settings.SettingsChanged += (_, _) => SaveSettings();
    }

    /// <summary>auto (default) / free / azure.</summary>
    public string Engine => Normalize(_engine.Value, "auto");

    public string SourceLanguage => Normalize(_sourceLanguage.Value, "auto");

    public string TargetLanguage => Normalize(_targetLanguage.Value, "auto");

    public string Endpoint =>
        string.IsNullOrWhiteSpace(_endpoint.Value) ? DefaultEndpoint : _endpoint.Value!.Trim();

    public string Region => _region.Value ?? string.Empty;

    public string Prefixes => _prefixes.Value ?? "tr,翻译";

    public bool EnableInlineFallback => _enableInlineFallback.Value;

    public int DebounceMs => Clamp(_debounceMs.Value, 0, 2000, 300);

    public int TimeoutSeconds => Clamp(_timeoutSeconds.Value, 1, 60, 5);

    public int CacheTtlMinutes => Clamp(_cacheTtlMinutes.Value, 0, 1440, 60);

    public int MaxResults => Clamp(_maxResults.Value, 1, 20, 5);

    /// <summary>Read from the DPAPI credential file or the CMDPAL_TRANSLATE_KEY environment variable.</summary>
    public static string ApiKey => SecretStore.LoadApiKey();

    public static string SettingsDirectory()
    {
        var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
        Directory.CreateDirectory(directory);
        return directory;
    }

    public static string SettingsJsonPath() => Path.Combine(SettingsDirectory(), "translate.settings.json");

    public static string CredentialPath() => Path.Combine(SettingsDirectory(), "translate.credential.bin");

    private static string N(string name) => NamespaceKey + "." + name;

    private static string Normalize(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.ToLowerInvariant();

    private static int Clamp(string? value, int min, int max, int fallback) =>
        int.TryParse(value, out var parsed) && parsed >= min && parsed <= max ? parsed : fallback;
}
