// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CmdPal.Ext.Translate.Core;

/// <summary>
/// Single orchestration point shared by the fallback item and the list page. Handles debounce,
/// cancellation of stale requests, timeout, caching and generation checks so out-of-order
/// responses never overwrite the newest result.
/// </summary>
public sealed class TranslationService
{
    private readonly ITranslationProvider _provider;
    private readonly TranslationCache _cache;
    private readonly TimeSpan _debounce;
    private readonly TimeSpan _timeout;
    private readonly object _gate = new();

    private CancellationTokenSource? _cts;
    private long _generation;
    private TranslationOutcome _current = TranslationOutcome.Idle();
    private TranslationRequest? _currentRequest;

    public TranslationService(
        ITranslationProvider provider,
        TranslationCache cache,
        TimeSpan debounce,
        TimeSpan timeout)
    {
        _provider = provider;
        _cache = cache;
        _debounce = debounce < TimeSpan.Zero ? TimeSpan.Zero : debounce;
        _timeout = timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(5) : timeout;
    }

    public event EventHandler<TranslationOutcome>? OutcomeChanged;

    public TranslationOutcome Current
    {
        get { lock (_gate) { return _current; } }
    }

    public TranslationRequest? CurrentRequest
    {
        get { lock (_gate) { return _currentRequest; } }
    }

    public void Request(string text, string from, string to)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            CancelInFlight();
            Publish(TranslationOutcome.Idle());
            return;
        }

        var request = new TranslationRequest(text, from, to);
        CancellationTokenSource cts;
        long generation;
        lock (_gate)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            cts = new CancellationTokenSource();
            _cts = cts;
            generation = ++_generation;
            _currentRequest = request;
        }

        Publish(Pending(request), generation);
        _ = RunAsync(request, generation, cts.Token);
    }

    /// <summary>Re-runs the current request with source and target exchanged.</summary>
    public void Swap()
    {
        var request = CurrentRequest;
        if (request is null)
        {
            return;
        }

        Request(request.Text, request.TargetLanguage, request.SourceLanguage);
    }

    public void Cancel()
    {
        CancelInFlight();
        Publish(TranslationOutcome.Idle());
    }

    private void CancelInFlight()
    {
        lock (_gate)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _generation++;
            _currentRequest = null;
        }
    }

    private async Task RunAsync(TranslationRequest request, long generation, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (_debounce > TimeSpan.Zero)
            {
                await Task.Delay(_debounce, token).ConfigureAwait(false);
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            if (!_provider.IsConfigured)
            {
                Publish(
                    new TranslationOutcome(
                        TranslationState.NoConfiguration,
                        request.Text,
                        null,
                        request.SourceLanguage,
                        request.TargetLanguage,
                        null,
                        stopwatch.ElapsedMilliseconds,
                        "no-key"),
                    generation);
                return;
            }

            if (_cache.TryGet(request.CacheKey, out var cached))
            {
                Publish(Success(request, cached, "cache", stopwatch.ElapsedMilliseconds), generation);
                return;
            }

            Publish(Pending(request, TranslationState.Translating), generation);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutCts.CancelAfter(_timeout);
            var translated = await _provider
                .TranslateAsync(request.Text, request.SourceLanguage, request.TargetLanguage, timeoutCts.Token)
                .ConfigureAwait(false);

            if (token.IsCancellationRequested || string.IsNullOrWhiteSpace(translated))
            {
                return;
            }

            _cache.Set(request.CacheKey, translated);
            Publish(Success(request, translated, _provider.Name, stopwatch.ElapsedMilliseconds), generation);
        }
        catch (OperationCanceledException)
        {
            // A newer query arrived, or the request timed out. Never publish stale results.
        }
        catch (Exception ex)
        {
            Publish(
                new TranslationOutcome(
                    TranslationState.Failed,
                    request.Text,
                    null,
                    request.SourceLanguage,
                    request.TargetLanguage,
                    _provider.Name,
                    stopwatch.ElapsedMilliseconds,
                    ex.GetType().Name),
                generation);
        }
    }

    private static TranslationOutcome Pending(TranslationRequest request, TranslationState state = TranslationState.Pending) =>
        new(
            state,
            request.Text,
            null,
            request.SourceLanguage,
            request.TargetLanguage,
            null,
            0,
            null);

    private static TranslationOutcome Success(
        TranslationRequest request,
        string translated,
        string engine,
        long elapsed) =>
        new(
            TranslationState.Success,
            request.Text,
            translated,
            request.SourceLanguage,
            request.TargetLanguage,
            engine,
            elapsed,
            null);

    private void Publish(TranslationOutcome outcome, long? generation = null)
    {
        lock (_gate)
        {
            if (generation.HasValue && generation.Value != _generation)
            {
                return;
            }

            _current = outcome;
        }

        OutcomeChanged?.Invoke(this, outcome);
    }
}
