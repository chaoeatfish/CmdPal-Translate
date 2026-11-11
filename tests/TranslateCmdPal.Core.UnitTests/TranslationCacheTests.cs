using Microsoft.CmdPal.Ext.Translate.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TranslateCmdPal.Core.UnitTests;

[TestClass]
public sealed class TranslationCacheTests
{
    [TestMethod]
    public void RoundTripAndClear()
    {
        var cache = new TranslationCache(TimeSpan.FromMinutes(5));
        cache.Set("zh>en:你好", "hello");
        Assert.IsTrue(cache.TryGet("zh>en:你好", out var value));
        Assert.AreEqual("hello", value);
        cache.Clear();
        Assert.IsFalse(cache.TryGet("zh>en:你好", out _));
    }

    [TestMethod]
    public void ZeroTtlExpiresImmediately()
    {
        var cache = new TranslationCache(TimeSpan.Zero);
        cache.Set("k", "v");
        Assert.IsFalse(cache.TryGet("k", out _));
    }

    [TestMethod]
    public void EmptyValuesAreIgnored()
    {
        var cache = new TranslationCache(TimeSpan.FromMinutes(1));
        cache.Set(string.Empty, "v");
        cache.Set("k", string.Empty);
        Assert.AreEqual(0, cache.Count);
    }
}
