using Microsoft.CmdPal.Ext.Translate.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TranslateCmdPal.Core.UnitTests;

[TestClass]
public sealed class TriggerParserTests
{
    [TestMethod]
    public void PrefixIsRequired()
    {
        Assert.IsTrue(TriggerParser.Parse("tr hello").IsMatch);
        Assert.AreEqual("hello", TriggerParser.Parse("tr hello").Text);
        Assert.IsFalse(TriggerParser.Parse("hello").IsMatch);
        Assert.IsFalse(TriggerParser.Parse("report.pdf").IsMatch);
        Assert.IsFalse(TriggerParser.Parse(string.Empty).IsMatch);
    }

    [TestMethod]
    public void ChinesePrefixAndEmptyText()
    {
        var parsed = TriggerParser.Parse("翻译 你好");
        Assert.IsTrue(parsed.IsMatch);
        Assert.AreEqual("你好", parsed.Text);
        Assert.IsTrue(TriggerParser.Parse("tr").IsMatch);
        Assert.AreEqual(string.Empty, TriggerParser.Parse("tr").Text);
    }

    [TestMethod]
    public void TargetOverrideLeadingAndTrailing()
    {
        var leading = TriggerParser.Parse("tr >en 你好");
        Assert.AreEqual("en", leading.TargetOverride);
        Assert.AreEqual("你好", leading.Text);

        var trailing = TriggerParser.Parse("tr hello >zh");
        Assert.AreEqual("zh", trailing.TargetOverride);
        Assert.AreEqual("hello", trailing.Text);
    }

    [TestMethod]
    public void ConfiguredPrefixesReplaceDefaults()
    {
        Assert.IsFalse(TriggerParser.Parse("tr hello", "fy,翻").IsMatch);
        Assert.IsTrue(TriggerParser.Parse("fy hello", "fy,翻").IsMatch);
    }

    [TestMethod]
    public void PageQueryDoesNotNeedPrefix()
    {
        var parsed = TriggerParser.ParsePageQuery("hello >en");
        Assert.IsTrue(parsed.IsMatch);
        Assert.AreEqual("hello", parsed.Text);
        Assert.AreEqual("en", parsed.TargetOverride);
        Assert.IsFalse(TriggerParser.ParsePageQuery("   ").IsMatch);
    }
}
