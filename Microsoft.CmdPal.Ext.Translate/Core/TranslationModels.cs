// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CmdPal.Ext.Translate.Core;

public enum TranslationState
{
    Idle,
    Pending,
    Translating,
    Success,
    NoConfiguration,
    Failed,
}

/// <summary>Credentials are supplied lazily so a key change takes effect without restarting the extension.</summary>
public sealed class TranslationCredentials
{
    public TranslationCredentials(string endpoint, string region, string apiKey)
    {
        Endpoint = endpoint;
        Region = region;
        ApiKey = apiKey;
    }

    public string Endpoint { get; }

    public string Region { get; }

    public string ApiKey { get; }
}

public sealed class TranslationRequest
{
    public TranslationRequest(string text, string sourceLanguage, string targetLanguage)
    {
        Text = text;
        SourceLanguage = sourceLanguage;
        TargetLanguage = targetLanguage;
    }

    public string Text { get; }

    public string SourceLanguage { get; }

    public string TargetLanguage { get; }

    public string CacheKey => SourceLanguage + ">" + TargetLanguage + ":" + Text;

    public TranslationRequest Swapped() => new(Text, TargetLanguage, SourceLanguage);
}

public sealed class TranslationOutcome
{
    public TranslationOutcome(
        TranslationState state,
        string sourceText,
        string? translatedText,
        string sourceLanguage,
        string targetLanguage,
        string? engine,
        long elapsedMilliseconds,
        string? error)
    {
        State = state;
        SourceText = sourceText;
        TranslatedText = translatedText;
        SourceLanguage = sourceLanguage;
        TargetLanguage = targetLanguage;
        Engine = engine;
        ElapsedMilliseconds = elapsedMilliseconds;
        Error = error;
    }

    public static TranslationOutcome Idle() =>
        new(TranslationState.Idle, string.Empty, null, "auto", "auto", null, 0, null);

    public TranslationState State { get; }

    public string SourceText { get; }

    public string? TranslatedText { get; }

    public string SourceLanguage { get; }

    public string TargetLanguage { get; }

    public string? Engine { get; }

    public long ElapsedMilliseconds { get; }

    public string? Error { get; }

    public string Direction => SourceLanguage + " → " + TargetLanguage;
}
