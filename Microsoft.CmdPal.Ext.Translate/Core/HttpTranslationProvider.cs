// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CmdPal.Ext.Translate.Core;

/// <summary>
/// Azure AI Translator v3 compatible endpoint (or any self-hosted endpoint with the same contract).
/// No key is ever stored in settings.json; it is supplied by the credential callback.
/// </summary>
public sealed partial class HttpTranslationProvider : ITranslationProvider, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Func<TranslationCredentials> _credentials;
    private readonly bool _ownsClient;

    public HttpTranslationProvider(Func<TranslationCredentials> credentials, HttpMessageHandler? handler = null)
    {
        _credentials = credentials;
        _ownsClient = true;
        _httpClient = handler is null ? new HttpClient() : new HttpClient(handler);
    }

    public string Name => "REST";

    public bool IsConfigured
    {
        get
        {
            var credentials = _credentials();
            return !string.IsNullOrWhiteSpace(credentials.ApiKey)
                && Uri.TryCreate(credentials.Endpoint, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
        }
    }

    public async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var credentials = _credentials();
        var url = credentials.Endpoint.TrimEnd('/')
            + "/translate?api-version=3.0&from="
            + Uri.EscapeDataString(sourceLanguage)
            + "&to="
            + Uri.EscapeDataString(targetLanguage);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Key", credentials.ApiKey);
        if (!string.IsNullOrWhiteSpace(credentials.Region))
        {
            request.Headers.TryAddWithoutValidation("Ocp-Apim-Subscription-Region", credentials.Region);
        }

        // Built with Utf8JsonWriter to stay trim/AOT friendly (no reflection-based serialization).
        using var payload = new MemoryStream();
        using (var writer = new Utf8JsonWriter(payload))
        {
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WriteString("Text", text);
            writer.WriteEndObject();
            writer.WriteEndArray();
        }

        request.Content = new ByteArrayContent(payload.ToArray());
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var translated = document.RootElement[0].GetProperty("translations")[0].GetProperty("text").GetString();
        if (string.IsNullOrEmpty(translated))
        {
            throw new InvalidOperationException("Translation response did not contain any text.");
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
