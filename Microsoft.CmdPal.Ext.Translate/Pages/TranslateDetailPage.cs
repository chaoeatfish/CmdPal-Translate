// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using Microsoft.CmdPal.Ext.Translate.Core;
using Microsoft.CmdPal.Ext.Translate.Helpers;
using Microsoft.CmdPal.Ext.Translate.Properties;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace Microsoft.CmdPal.Ext.Translate.Pages;

/// <summary>Long translations are shown on a markdown detail page rather than truncated in a list row.</summary>
internal sealed partial class TranslateDetailPage : ContentPage
{
    private readonly TranslationOutcome _outcome;

    public TranslateDetailPage(TranslationOutcome outcome)
    {
        _outcome = outcome;
        Icon = Icons.Detail;
        Title = Resources.Detail;
        Name = Resources.Open;
    }

    public override IContent[] GetContent()
    {
        var translated = _outcome.TranslatedText ?? Resources.NoConfig;
        var markdown = "### " + _outcome.Direction
            + "\n\n"
            + translated
            + "\n\n---\n\n**" + Resources.CopySource + "**\n\n```text\n"
            + _outcome.SourceText
            + "\n```";
        return [new MarkdownContent(markdown)];
    }
}
