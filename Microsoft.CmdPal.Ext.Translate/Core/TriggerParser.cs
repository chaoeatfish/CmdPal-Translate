// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.CmdPal.Ext.Translate.Core;

public sealed class ParsedQuery
{
    public ParsedQuery(bool isMatch, string text, string? targetOverride)
    {
        IsMatch = isMatch;
        Text = text;
        TargetOverride = targetOverride;
    }

    public bool IsMatch { get; }

    public string Text { get; }

    public string? TargetOverride { get; }

    public static ParsedQuery None => new(false, string.Empty, null);
}

/// <summary>
/// Parses the explicit trigger. A query without a configured prefix never matches, which keeps
/// this extension's fallback item out of results produced by other extensions (Everything in particular).
/// </summary>
public static class TriggerParser
{
    public const string DefaultPrefix = "tr";

    private static readonly string[] DefaultPrefixes = ["tr", "翻译"];

    private static readonly HashSet<string> AllowedTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "auto", "zh", "en", "ja", "ko", "fr", "de", "es", "ru",
    };

    public static IReadOnlyList<string> Prefixes(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return DefaultPrefixes;
        }

        var parts = configured.Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? DefaultPrefixes : parts;
    }

    /// <summary>Home-page query. Requires an explicit prefix such as "tr hello".</summary>
    public static ParsedQuery Parse(string? query, string? configuredPrefixes = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return ParsedQuery.None;
        }

        var text = query.Trim();
        var matched = false;
        foreach (var prefix in Prefixes(configuredPrefixes))
        {
            if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                text = string.Empty;
                matched = true;
                break;
            }

            if (text.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
            {
                text = text[(prefix.Length + 1)..].Trim();
                matched = true;
                break;
            }
        }

        return matched ? ApplyOverrides(text) : ParsedQuery.None;
    }

    /// <summary>Inside the extension's own page the whole search box is the text to translate.</summary>
    public static ParsedQuery ParsePageQuery(string? query) =>
        string.IsNullOrWhiteSpace(query) ? ParsedQuery.None : ApplyOverrides(query.Trim());

    private static ParsedQuery ApplyOverrides(string text)
    {
        string? target = null;

        var leading = LeadingOverride(text);
        if (leading is { } lead)
        {
            target = lead.Lang;
            text = lead.Remainder.Trim();
        }
        else
        {
            var trailing = TrailingOverride(text);
            if (trailing is { } tail)
            {
                target = tail.Lang;
                text = tail.Remainder.TrimEnd();
            }
        }

        return new ParsedQuery(true, text.Trim(), target);
    }

    private static (string Lang, string Remainder)? LeadingOverride(string text)
    {
        if (text.Length < 2 || text[0] != '>')
        {
            return null;
        }

        var space = text.IndexOf(' ');
        var token = space < 0 ? text[1..] : text[1..space];
        if (!AllowedTargets.Contains(token))
        {
            return null;
        }

        return (token.ToLowerInvariant(), space < 0 ? string.Empty : text[(space + 1)..]);
    }

    private static (string Lang, string Remainder)? TrailingOverride(string text)
    {
        var marker = text.LastIndexOf('>');
        if (marker < 1 || marker == text.Length - 1)
        {
            return null;
        }

        var token = text[(marker + 1)..];
        if (!AllowedTargets.Contains(token))
        {
            return null;
        }

        return (token.ToLowerInvariant(), text[..marker]);
    }
}
