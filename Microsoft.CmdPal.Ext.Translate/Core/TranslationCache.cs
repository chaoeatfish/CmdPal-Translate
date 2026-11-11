// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;

namespace Microsoft.CmdPal.Ext.Translate.Core;

public sealed class TranslationCache
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly TimeSpan _ttl;

    public TranslationCache(TimeSpan ttl)
    {
        _ttl = ttl;
    }

    public int Count => _entries.Count;

    public bool TryGet(string key, out string value)
    {
        value = string.Empty;
        if (!_entries.TryGetValue(key, out var entry))
        {
            return false;
        }

        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _entries.TryRemove(key, out _);
            return false;
        }

        value = entry.Value;
        return true;
    }

    public void Set(string key, string value)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            return;
        }

        _entries[key] = new Entry(value, DateTimeOffset.UtcNow + _ttl);
    }

    public void Clear() => _entries.Clear();

    private sealed class Entry
    {
        public Entry(string value, DateTimeOffset expiresAt)
        {
            Value = value;
            ExpiresAt = expiresAt;
        }

        public string Value { get; }

        public DateTimeOffset ExpiresAt { get; }
    }
}
