// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CmdPal.Ext.Translate.Core;

/// <summary>
/// Provider boundary. v1 ships one REST implementation; an on-device model can be added later
/// without touching the page, command or fallback code.
/// </summary>
public interface ITranslationProvider
{
    string Name { get; }

    bool IsConfigured { get; }

    Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken);
}
