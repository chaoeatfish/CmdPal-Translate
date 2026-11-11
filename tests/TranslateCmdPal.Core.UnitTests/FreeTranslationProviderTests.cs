using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CmdPal.Ext.Translate.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TranslateCmdPal.Core.UnitTests;

[TestClass]
public sealed class FreeTranslationProviderTests
{
    [TestMethod]
    public async Task FreeEngineParsesResponse()
    {
        var handler = new StubHandler();
        using var provider = new FreeTranslationProvider(handler);
        Assert.IsTrue(provider.IsConfigured);
        var result = await provider.TranslateAsync("你好", "zh", "en", CancellationToken.None);
        Assert.AreEqual("hello", result);
        StringAssert.Contains(handler.LastUri ?? string.Empty, "langpair=zh");
    }

    [TestMethod]
    public async Task AutoEngineUsesFreeWhenNoKeyIsPresent()
    {
        using var provider = new CompositeTranslationProvider(
            () => new TranslationCredentials("https://example.com", string.Empty, string.Empty),
            () => "auto",
            azureHandler: new StubHandler(),
            freeHandler: new StubHandler());
        Assert.IsTrue(provider.IsConfigured);
        Assert.AreEqual("Free", provider.Name);
        Assert.AreEqual("hello", await provider.TranslateAsync("你好", "zh", "en", CancellationToken.None));
    }

    [TestMethod]
    public void ExplicitAzureWithoutKeyIsNotConfigured()
    {
        using var provider = new CompositeTranslationProvider(
            () => new TranslationCredentials("https://example.com", string.Empty, string.Empty),
            () => "azure",
            azureHandler: new StubHandler(),
            freeHandler: new StubHandler());
        Assert.IsFalse(provider.IsConfigured);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public string? LastUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUri = request.RequestUri?.ToString();
            var body = "{\"responseData\":{\"translatedText\":\"hello\"},\"responseStatus\":200}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body),
            });
        }
    }
}
