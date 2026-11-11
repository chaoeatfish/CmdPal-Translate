using Microsoft.CmdPal.Ext.Translate.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TranslateCmdPal.Core.UnitTests;

[TestClass]
public sealed class LanguageDetectorTests
{
    [TestMethod]
    public void DetectsChineseAndEnglish()
    {
        Assert.AreEqual("zh", LanguageDetector.Detect("水土保持方案"));
        Assert.AreEqual("en", LanguageDetector.Detect("water and soil conservation"));
        Assert.AreEqual("en", LanguageDetector.Detect(string.Empty));
    }

    [TestMethod]
    public void AutoResolvesOppositeSide()
    {
        var (fromZh, toZh) = LanguageDetector.Resolve("水土保持", "auto", "auto", null);
        Assert.AreEqual("zh", fromZh);
        Assert.AreEqual("en", toZh);

        var (fromEn, toEn) = LanguageDetector.Resolve("soil conservation", "auto", "auto", null);
        Assert.AreEqual("en", fromEn);
        Assert.AreEqual("zh", toEn);
    }

    [TestMethod]
    public void OverrideAndFixedSettingsWin()
    {
        var (from, to) = LanguageDetector.Resolve("水土保持", "auto", "auto", "en");
        Assert.AreEqual("zh", from);
        Assert.AreEqual("en", to);

        var (_, fixedTo) = LanguageDetector.Resolve("soil", "en", "zh", null);
        Assert.AreEqual("zh", fixedTo);

        var (sameFrom, sameTo) = LanguageDetector.Resolve("soil", "en", "en", null);
        Assert.AreEqual("en", sameFrom);
        Assert.AreEqual("zh", sameTo);
    }
}
