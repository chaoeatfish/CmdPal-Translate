using Microsoft.CmdPal.Ext.Translate.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TranslateCmdPal.Core.UnitTests;

[TestClass]
public sealed class TranslationServiceTests
{
    [TestMethod]
    public async Task WithoutCredentialsNoNetworkRequestIsMade()
    {
        var provider = new FakeProvider { IsConfigured = false };
        var service = Create(provider);
        var wait = WaitFor(service, o => o.State == TranslationState.NoConfiguration);
        service.Request("hello", "en", "zh");
        var outcome = await wait;
        Assert.AreEqual(0, provider.Calls);
        Assert.AreEqual("no-key", outcome.Error);
    }

    [TestMethod]
    public async Task StaleResponseNeverOverwritesNewestQuery()
    {
        var provider = new FakeProvider { DelayMs = 120 };
        var service = Create(provider);
        var wait = WaitFor(service, o => o.State == TranslationState.Success && o.SourceText == "b");
        service.Request("a", "en", "zh");
        service.Request("b", "en", "zh");
        var outcome = await wait;
        Assert.AreEqual("b", outcome.SourceText);
    }

    [TestMethod]
    public async Task RepeatedQueryUsesCache()
    {
        var provider = new FakeProvider();
        var service = Create(provider);
        var first = WaitFor(service, o => o.State == TranslationState.Success && o.Engine == "REST");
        service.Request("hello", "en", "zh");
        await first;

        var cached = WaitFor(service, o => o.State == TranslationState.Success && o.Engine == "cache");
        service.Request("hello", "en", "zh");
        var outcome = await cached;
        Assert.AreEqual("T:hello", outcome.TranslatedText);
        Assert.AreEqual(1, provider.Calls);
    }

    [TestMethod]
    public async Task ProviderFailureBecomesAFailedOutcome()
    {
        var provider = new FakeProvider { Throw = true };
        var service = Create(provider);
        var wait = WaitFor(service, o => o.State == TranslationState.Failed);
        service.Request("hello", "en", "zh");
        var outcome = await wait;
        Assert.AreEqual("InvalidOperationException", outcome.Error);
    }

    [TestMethod]
    public async Task SwapReversesTheDirection()
    {
        var provider = new FakeProvider();
        var service = Create(provider);
        var first = WaitFor(service, o => o.State == TranslationState.Success);
        service.Request("hello", "en", "zh");
        await first;

        var swapped = WaitFor(service, o => o.State == TranslationState.Success && o.SourceLanguage == "zh");
        service.Swap();
        var outcome = await swapped;
        Assert.AreEqual("en", outcome.TargetLanguage);
    }

    private static TranslationService Create(FakeProvider provider) =>
        new(provider, new TranslationCache(TimeSpan.FromMinutes(5)), TimeSpan.Zero, TimeSpan.FromSeconds(3));

    private static async Task<TranslationOutcome> WaitFor(
        TranslationService service,
        Func<TranslationOutcome, bool> predicate,
        int timeoutMs = 5000)
    {
        var completion = new TaskCompletionSource<TranslationOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<TranslationOutcome>? handler = null;
        handler = (_, outcome) =>
        {
            if (predicate(outcome))
            {
                completion.TrySetResult(outcome);
            }
        };
        service.OutcomeChanged += handler;

        var finished = await Task.WhenAny(completion.Task, Task.Delay(timeoutMs));
        service.OutcomeChanged -= handler;
        Assert.IsTrue(finished == completion.Task, "Timed out waiting for a translation outcome.");
        return await completion.Task;
    }

    private sealed class FakeProvider : ITranslationProvider
    {
        public bool IsConfigured { get; set; } = true;

        public bool Throw { get; set; }

        public int DelayMs { get; set; }

        public int Calls { get; private set; }

        public string Name => "REST";

        public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken)
        {
            Calls++;
            if (DelayMs > 0)
            {
                await Task.Delay(DelayMs, cancellationToken);
            }

            if (Throw)
            {
                throw new InvalidOperationException("boom");
            }

            return "T:" + text;
        }
    }
}
