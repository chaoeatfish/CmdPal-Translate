// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.

using System.Globalization;
using System.Resources;

namespace Microsoft.CmdPal.Ext.Translate.Properties;

/// <summary>Strongly typed access to Resources.resx. Add translated .resx files here when localizing.</summary>
internal sealed class Resources
{
    private static readonly ResourceManager Manager =
        new("Microsoft.CmdPal.Ext.Translate.Properties.Resources", typeof(Resources).Assembly);

    private static string Get(string name, string fallback) =>
        Manager.GetString(name, CultureInfo.CurrentUICulture) ?? fallback;

    public static string ExtensionName => Get("ExtensionName", "翻译");
    public static string CommandSubtitle => Get("CommandSubtitle", "中英互译 · 输入 tr 文本");
    public static string Open => Get("Open", "打开");
    public static string PlaceholderText => Get("PlaceholderText", "输入待翻译文本");
    public static string CopyTranslation => Get("CopyTranslation", "复制译文");
    public static string CopySource => Get("CopySource", "复制原文");
    public static string Swap => Get("Swap", "交换语言方向");
    public static string TranslateClipboard => Get("TranslateClipboard", "翻译剪贴板");
    public static string ImportKey => Get("ImportKey", "从剪贴板导入 API 密钥");
    public static string Settings => Get("Settings", "设置");
    public static string Detail => Get("Detail", "查看详情");
    public static string Translating => Get("Translating", "翻译中…");
    public static string Pending => Get("Pending", "等待翻译");
    public static string Idle => Get("Idle", "输入 tr 文本 开始翻译");
    public static string NoConfig => Get("NoConfig", "尚未配置翻译服务");
    public static string NoConfigSubtitle => Get("NoConfigSubtitle", "请配置翻译服务");
    public static string Failed => Get("Failed", "翻译失败");
    public static string FailedSubtitle => Get("FailedSubtitle", "检查配置后重试");
    public static string ClipboardEmpty => Get("ClipboardEmpty", "剪贴板为空或不含文本");
    public static string KeySaved => Get("KeySaved", "API 密钥已保存");
    public static string Copied => Get("Copied", "已复制到剪贴板");
    public static string KeyImportFailed => Get("KeyImportFailed", "密钥保存失败");
    public static string CacheHit => Get("CacheHit", "缓存");
    public static string LangAuto => Get("LangAuto", "自动识别");
    public static string LangZh => Get("LangZh", "中文");
    public static string LangEn => Get("LangEn", "英文");
    public static string EngineAuto => Get("EngineAuto", "自动");
    public static string EngineFree => Get("EngineFree", "免费");
    public static string EngineAzure => Get("EngineAzure", "Azure");
    public static string SettingEngine => Get("SettingEngine", "翻译引擎");
    public static string SettingEngineDesc => Get("SettingEngineDesc", "默认自动");
    public static string SettingSource => Get("SettingSource", "源语言");
    public static string SettingSourceDesc => Get("SettingSourceDesc", "源语言");
    public static string SettingTarget => Get("SettingTarget", "目标语言");
    public static string SettingTargetDesc => Get("SettingTargetDesc", "目标语言");
    public static string SettingEndpoint => Get("SettingEndpoint", "翻译端点");
    public static string SettingEndpointDesc => Get("SettingEndpointDesc", "端点");
    public static string SettingRegion => Get("SettingRegion", "服务区域");
    public static string SettingRegionDesc => Get("SettingRegionDesc", "区域");
    public static string SettingPrefixes => Get("SettingPrefixes", "触发前缀");
    public static string SettingPrefixesDesc => Get("SettingPrefixesDesc", "前缀");
    public static string SettingInline => Get("SettingInline", "启用首页内联结果");
    public static string SettingInlineDesc => Get("SettingInlineDesc", "内联结果");
    public static string SettingDebounce => Get("SettingDebounce", "防抖（毫秒）");
    public static string SettingDebounceDesc => Get("SettingDebounceDesc", "防抖");
    public static string SettingTimeout => Get("SettingTimeout", "请求超时（秒）");
    public static string SettingTimeoutDesc => Get("SettingTimeoutDesc", "超时");
    public static string SettingCacheTtl => Get("SettingCacheTtl", "缓存有效期（分钟）");
    public static string SettingCacheTtlDesc => Get("SettingCacheTtlDesc", "缓存");
    public static string SettingMaxResults => Get("SettingMaxResults", "最大结果数");
    public static string SettingMaxResultsDesc => Get("SettingMaxResultsDesc", "结果数");
}
