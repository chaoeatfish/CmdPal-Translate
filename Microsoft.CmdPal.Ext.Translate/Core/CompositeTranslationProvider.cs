// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CmdPal.Ext.Translate.Core;

/// <summary>
/// Engine selector. "auto" uses Azure when a key is configured, otherwise the no-key engine;
/// a failed Azure call in auto mode falls back to the no-key engine.
/// </summary>
public sealed partial class CompositeTranslationProvider : ITranslationProvider, IDisposable
{
    private readonly HttpTranslationProvider _azure;
    private readonly FreeTranslationProvider _free;
    private readonly Func<string> _engineSelector;

    public CompositeTranslationProvider(
        Func<TranslationCredentials> credentials,
        Func<string> engineSelector,
        HttpMessageHandler? azureHandler = null,
        HttpMessageHandler? freeHandler = null)
    {
        _azure = new HttpTranslationProvider(credentials, azureHandler);
        _free = new FreeTranslationProvider(freeHandler);
        _engineSelector = engineSelector;
    }

    /// <summary>Returns the engine actually in use, so the result subtitle is accurate.</summary>
    public string Name
    {
        get
        {
            var engine = _engineSelector();
            if (engine == "azure")
            {
                return _azure.Name;
            }

            if (engine == "free")
            {
                return _free.Name;
            }

            return _azure.IsConfigured ? _azure.Name : _free.Name;
        }
    }

    public bool IsConfigured => _engineSelector() == "azure" ? _azure.IsConfigured : true;

    public async Task<string> TranslateAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken cancellationToken)
    {
        var engine = _engineSelector();
        ITranslationProvider provider;
        if (engine == "azure")
        {
            provider = _azure;
        }
        else if (engine == "free")
        {
            provider = _free;
        }
        else
        {
            provider = _azure.IsConfigured ? _azure : _free;
        }

        try
        {
            return await provider.TranslateAsync(text, sourceLanguage, targetLanguage, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (engine == "auto" && provider == _azure)
        {
            return await _free.TranslateAsync(text, sourceLanguage, targetLanguage, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _azure.Dispose();
        _free.Dispose();
    }
}
