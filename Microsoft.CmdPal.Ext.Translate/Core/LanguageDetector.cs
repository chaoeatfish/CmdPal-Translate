// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;

namespace Microsoft.CmdPal.Ext.Translate.Core;

public static class LanguageDetector
{
    /// <summary>Heuristic zh/en detection. Deliberately dependency-free and unit-testable.</summary>
    public static string Detect(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "en";
        }

        var cjk = 0;
        var latin = 0;
        foreach (var ch in text)
        {
            if (ch >= '\u3400' && ch <= '\u9FFF')
            {
                cjk++;
            }
            else if (char.IsLetter(ch))
            {
                latin++;
            }
        }

        if (cjk == 0)
        {
            return "en";
        }

        return (double)cjk / (cjk + latin) >= 0.15 ? "zh" : "en";
    }

    /// <summary>
    /// Resolves a direction. With "auto" settings the target is the opposite side of the detected source,
    /// which implements the zh-CN ⇄ en-US default. An explicit &gt;xx token always wins.
    /// </summary>
    public static (string From, string To) Resolve(
        string text,
        string configuredSource,
        string configuredTarget,
        string? targetOverride)
    {
        var from = IsAuto(configuredSource) ? Detect(text) : configuredSource.ToLowerInvariant();
        string to;

        if (!string.IsNullOrWhiteSpace(targetOverride) && !IsAuto(targetOverride!))
        {
            to = targetOverride!.ToLowerInvariant();
        }
        else if (!IsAuto(configuredTarget))
        {
            to = configuredTarget.ToLowerInvariant();
        }
        else
        {
            to = from == "zh" ? "en" : "zh";
        }

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            to = from == "zh" ? "en" : "zh";
        }

        return (from, to);
    }

    private static bool IsAuto(string value) =>
        string.IsNullOrWhiteSpace(value) || string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase);
}
