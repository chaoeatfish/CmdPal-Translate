// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CmdPal.Ext.Translate.Core;

/// <summary>
/// No-key default engine (MyMemory public API). Lets the extension work immediately after
/// installation; an Azure AI Translator endpoint can be configured later for higher quotas.
/// Text is sent to a third-party service — see the usage notes.
/// </summary>
public sealed partial class FreeTranslationProvider : ITranslationProvider, IDisposable
{
    private const string Endpoint = "https://api.mymemory.translated.net/get";
    private readonly HttpClient _httpClient;
    private readonly bool _ownsClient;

    public FreeTranslationProvider(HttpMessageHandler? handler = null)
    {
        _httpClient = handler is null ? new HttpClient() : new HttpClient(handler);
        _ownsClient = true;
    }

    public string Name => "Free";

    public bool IsConfigured => true;

    public async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var pair = Uri.EscapeDataString(sourceLanguage) + "|" + Uri.EscapeDataString(targetLanguage);
        var url = Endpoint + "?q=" + Uri.EscapeDataString(text) + "&langpair=" + pair;

        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;

        if (root.TryGetProperty("responseStatus", out var status) && status.ValueKind == JsonValueKind.Number && status.GetInt32() != 200)
        {
            throw new InvalidOperationException("Free translation service rejected the request.");
        }

        var translated = root.GetProperty("responseData").GetProperty("translatedText").GetString();
        if (string.IsNullOrWhiteSpace(translated))
        {
            throw new InvalidOperationException("Free translation service returned no text.");
        }

        return translated;
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _httpClient.Dispose();
        }
    }
}
