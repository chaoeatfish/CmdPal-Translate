# TranslateCmdPal — 命令面板翻译扩展

> **当前状态（2026-11-11）**：MSIX 包 `0.0.3.0` 已成功安装（Status Ok），安装脚本的路径 bug 已修复。
> 但命令面板在 COM 激活扩展时返回 `0x80070005`（拒绝访问），扩展**尚未出现在面板里**。
> 下一步：重启电脑 → 命令面板输入 `Reload` → 选「Reload Command Palette extensions」。
> 面向普通用户的使用说明见 **`使用说明.html`**。

独立 MSIX 扩展，基于 `microsoft/PowerToys` 官方 `ExtensionTemplate/TemplateCmdPalExtension` 骨架生成。与现有 Everything 扩展**并列注册、进程隔离**：不修改 Everything 的包、清单、CLSID、设置、结果排序或命令栏，也不修改命令面板宿主与 SDK。

## 前提（本实现遵循的约定）

| 项 | 实现 |
|---|---|
| 语言方向 | 默认 zh-CN ⇄ en-US 自动判源，目标取源的对侧；设置可固定源/目标；查询内 `>en` / `>zh` 覆盖目标。 |
| 触发方式 | 顶层命令「翻译」；首页内联结果**必须**命中前缀 `tr ` / `翻译 `（可配置）。未命中时 fallback 返回空，不会挤占 Everything 的内联结果。不注册全局热键、不自动读剪贴板。 |
| 译文展示 | 顶层命令 → `DynamicListPage` 实时列表；首页内联 `ListItem`（Title=译文，Subtitle=方向·引擎·耗时）；`Enter` 复制译文；右下命令栏含复制原文 / 交换方向 / 详情 / 翻译剪贴板 / 导入密钥 / 设置；长文用 `MarkdownContent` 详情页。 |

## 本机环境（已实测）

| 项 | 值 |
|---|---|
| 系统 | Windows 10 企业版 19045（x64），当前用户非管理员 |
| PowerToys / 命令面板 | 0.102.3101.0 / `Microsoft.CommandPalette 0.13.13096.0`（.NET 10.0.11） |
| 你现有的 Everything 扩展 | `Goucey.EverythingExtensionforCommandPalette 0.1.11.0`，扩展 SDK **0.9.260303001** |
| .NET SDK | 原 8.0.425；已用户级加装 10.0.404（`%LOCALAPPDATA%\Microsoft\dotnet10`） |
| 打包工具 | Windows Kits 10.0.26100.0 的 `makeappx.exe` / `signtool.exe` |

## 目录

```
TranslateCmdPal/
├─ 使用说明.html                          # 面向普通用户的使用说明
├─ install/
│  ├─ Install-ForMe.cmd                   # 双击 → 自动请求管理员权限 → 安装
│  ├─ FinishInstall.ps1                   # 导入证书 + 启用旁加载 + Add-AppxPackage
│  ├─ TranslateCmdPal.msix                # 已构建、已签名的安装包
│  ├─ dev-signing.cer / layout/           # 自签名证书 / 包内容
│  └─ install-result.txt                  # 安装结果（运行后生成）
├─ Microsoft.CmdPal.Ext.Translate/        # 扩展工程
│  ├─ Program.cs / TranslateExtension.cs  # COM 服务器 + IExtension
│  ├─ TranslateCommandsProvider.cs        # Id / 顶层命令 / 门控 fallback / 设置
│  ├─ FallbackTranslateItem.cs            # 首页内联结果（前缀门控）
│  ├─ Core/                               # 纯 .NET 逻辑（前缀、语种、缓存、编排、引擎）
│  ├─ Commands/ Pages/ Helpers/ Properties/
│  └─ Package.appxmanifest                # 独立 Identity / AppExtension Id / CLSID
├─ tests/TranslateCmdPal.Core.UnitTests/  # net8.0 单测，链接 Core 源码
├─ repo-integration/                      # 合入 PowerToys 仓库的清单改动与脚本
├─ tools/verify.py                        # 静态校验
└─ Microsoft.CmdPal.Ext.Translate.sln
```

## 安装状态

已完成：`Install-ForMe.cmd` 曾因 `-RootDir "%~dp0"` 尾部反斜杠把引号带进路径而报「路径中具有非法字符」，**已修复**（改用 `$PSScriptRoot`）。用户授权那次已成功写入旁加载策略与根证书；随后 `Add-AppxPackage` 安装成功（`0.0.3.0`，Status Ok），`install-result.txt` 记录结果。

未完成：命令面板日志 `Log_2026-11-11.log` 持续出现

```
Failed to find 翻译: -2147024891        // 0x80070005 E_ACCESSDENIED
Failed to start extension ... System.ArgumentException: You forgot to start the extension
```

已排除的因素：构建产物（改用 build 输出重打包）、清单属性（对齐 Everything 扩展的 `uap18` 与 `PackageDependency`）、部署方式（签名 MSIX 与 VS 式松散注册均试过）、进程竞态（重启 CmdPal 后复现）、COM 服务器本身（直接运行 exe 可正常常驻）、开发者许可证（`Get-WindowsDeveloperLicense` IsValid=True）、旁加载策略（已开启）。Everything 扩展每次都正常加载 `Loaded 2 command(s) and 1 band(s)`。

结论：Windows 10 19045 上，CmdPal 0.13 通过 `CoCreateInstance(CLSCTX_LOCAL_SERVER, IID_IExtension)` 激活**自签名侧载包**的 packaged COM 服务器时被系统拒绝；商店签名的扩展不受此限制。这是宿主/平台限制，不是工程代码错误。

下一步：重启电脑后执行 `Reload`；若仍失败，需改用商店签名分发或升级系统/PowerToys。卸载方式见使用说明第五节，Everything 插件不受影响。

## 使用

见 `使用说明.html`。最常用：

| 输入 | 含义 |
|---|---|
| `tr hello` / `tr 你好` | 自动判源互译，`Enter` 复制译文 |
| `翻译 水土保持方案` | 中文前缀 |
| `tr >en 你好` / `tr hello >zh` | 强制目标语言 |
| 打开「翻译」后直接输入 | 页面内整段翻译，无需前缀 |
| `report.pdf`（无前缀） | 本扩展不产生结果，Everything 行为不变 |

## 翻译引擎与隐私

- **默认「自动 / 免费」**：无需账号与密钥，装好即用，走 MyMemory 公共接口（`api.mymemory.translated.net`）。文本会发送给第三方，**不要翻译敏感内容**。
- **可选 Azure**：设置「翻译引擎」为 Azure，复制密钥 → 命令栏「从剪贴板导入 API 密钥」。密钥以 DPAPI 加密写入 `translate.credential.bin`，不进 `translate.settings.json`、不进日志；也支持环境变量 `CMDPAL_TRANSLATE_KEY`。
- 设置项：翻译引擎、源/目标语言、端点、区域、触发前缀、启用内联、防抖(300ms)、超时(5s)、缓存(60min)、最大结果数。命名空间 `translate.*`。

## 兼容性要点

- 新 `Identity Name=Microsoft.CmdPal.Ext.Translate`、新 `AppExtension Id=TranslateCmdPal`、新 CLSID `7F019E71-4A9D-4C06-B035-8272FD1EE90F`（`[Guid]` / `com:Class Id` / `CreateInstance ClassId` 三处一致），**不复用** `Id=PowerToys` / `7EC02C7D-…`。
- `SupportedInterfaces` 只用 `Commands`；不实现 Dock / `ICommandProvider3|4`；申请 `internetClient` + `runFullTrust`，不申请 `systemAIModels`。
- **SDK 锁 `Microsoft.CommandPalette.Extensions 0.9.260303001`**，与本机已正常工作的 Everything 扩展完全一致，规避宿主 0.13 与新版扩展 SDK 的 `E_NOINTERFACE` 风险。目标框架 `net10.0-windows10.0.26100.0`，最低系统版本 19041。
- 请求 300ms 防抖 + `CancellationTokenSource` + 超时 + 缓存 + 生成号校验；`GetItems()` 不做网络 IO；断网/401/429 降级为提示条目。
- 不注册自定义快捷键（Everything 扩展已证实 CmdPal 对自定义热键支持不稳定）。

## 构建（本机已完成）

```bash
# 用户级 .NET 10 SDK
"%LOCALAPPDATA%\Microsoft\dotnet10\dotnet.exe" build \
  Microsoft.CmdPal.Ext.Translate/Microsoft.CmdPal.Ext.Translate.csproj -c Debug -p:Platform=x64
"%LOCALAPPDATA%\Microsoft\dotnet10\dotnet.exe" publish ... -p:PublishProfile=win-x64.pubxml
# 再补齐 AppxManifest.xml + Assets，makeappx pack，signtool sign
```

## 验证结果（2026-11-11）

| 项 | 结果 |
|---|---|
| `dotnet test`（net8.0，19 项） | **19/19 通过，0 警告** |
| 真实 SDK 0.9 编译主工程 | **0 错误 0 警告** |
| `python tools/verify.py` | 全部通过 |
| `repo-integration/integrate.py` 预演 + 实跑 | 3 个 `.slnf` + `PowerToys.slnx` 共 7 条工程条目正确写入 |
| `makeappx pack` / `signtool sign` | 均成功，产出 `install/TranslateCmdPal.msix` |
| `Add-AppxPackage` | 首次 `0x800B0109`（证书未信任）→ 用户授权后成功；当前 `0.0.3.0 Status Ok` |
| 命令面板加载 | **失败**：`CoCreateInstance` 返回 `0x80070005`，见「安装状态」 |

## 已知限制

- **本机 Windows 10 19045 + CmdPal 0.13 无法激活自签名侧载扩展的 packaged COM 服务器**（`E_ACCESSDENIED`）。代码与官方可工作的扩展一致，属平台限制；重启电脑后再 `Reload` 是最后一试。
- 免费引擎有公共额度限制且需外网；敏感内容请改用自建/Azure 端点。
- 页面与 fallback 共享同一 `TranslationService`：页面内输入时首页内联项同步刷新，属预期行为。
- 包由自签名开发证书签名，仅适合本机使用；分发到其它机器需重新信任证书。
