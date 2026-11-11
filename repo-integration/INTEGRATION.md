# 合入 microsoft/PowerToys 仓库的清单改动

交付物是可独立部署的 MSIX 扩展。若要把它作为仓库内工程随 PowerToys 一起构建，只需以下**清单式**改动；宿主 UI、SDK、稀疏包清单和 Everything 扩展均不动。

## 1. 落位

把 `Microsoft.CmdPal.Ext.Translate/` 复制到 `src/modules/cmdpal/ext/Microsoft.CmdPal.Ext.Translate/`，把 `tests/TranslateCmdPal.Core.UnitTests/` 复制到 `src/modules/cmdpal/Tests/Microsoft.CmdPal.Ext.Translate.UnitTests/`（并把 csproj 里的 `Compile Include` 相对路径从 `../../Microsoft.CmdPal.Ext.Translate/Core/**/*.cs` 改为 `../../ext/Microsoft.CmdPal.Ext.Translate/Core/**/*.cs`）。

保持 `Microsoft.CmdPal.Ext.Translate.sln` 为独立开发用解决方案；仓库构建用 `.slnf` / `.slnx`。

## 2. 必须修改的仓库文件

| 文件 | 改动 |
|---|---|
| `PowerToys.slnx` | 追加两个 `<Project Path="…" />`：扩展工程 + 单测工程 |
| `src/modules/cmdpal/CommandPalette.slnf` | `solution.projects` 追加两个工程路径 |
| `src/modules/cmdpal/CommandPalette - no UI tests.slnf` | 同上 |
| `src/modules/cmdpal/Microsoft.CmdPal.Ext.PowerToys.slnf` | 仅追加扩展工程（按该文件用途） |
| `Directory.Packages.props`（仓库根 / `ext/` 对应层级） | 确认 `Microsoft.CommandPalette.Extensions`、`Microsoft.Windows.CsWinRT`、`Shmuelie.WinRTServer`、`Microsoft.Windows.SDK.BuildTools.MSIX`、`MSTest.*` 已有 `PackageVersion`；缺失才补 |
| `NOTICE.md` + `.pipelines/verifyNoticeMdAgainstNugetPackages.ps1` | 仅当引入新 NuGet 包时补条目。本扩展只用模板已有的 4 个包，通常无需改动 |
| 安装器文件清单 | 仅当随安装包分发时，把输出加入 `WinUI3Apps\CmdPalExtensions\<工程名>` 的打包项（参照 `SamplePagesExtension` 的条目） |
| `src/modules/cmdpal/doc/` | 增加扩展说明（触发语法、设置项、密钥导入） |

`integrate.py` 自动完成前三个 `.slnf` 与 `.slnx` 的追加；其余为人工确认项。

## 3. 明确**不**修改的文件

- `src/PackageIdentity/AppxManifest.xml`：内置扩展由 `Microsoft.CmdPal.Ext.PowerToys.exe` 聚合，使用 `Id=PowerToys` + CLSID `7EC02C7D-…`。本扩展有独立清单与 CLSID，不走稀疏包。
- `src/modules/cmdpal/ext/Microsoft.CmdPal.Ext.PowerToys/*`：不把 provider 并入内置聚合宿主。
- `src/modules/cmdpal/Microsoft.CmdPal.UI*`、`Microsoft.CmdPal.UI.ViewModels/*`、`extensionsdk/*`。
- 任何 Everything 扩展文件（不在本仓库 `main` 的 `ext/` 下；ECP / EverythingPal 为独立分发包）。

## 4. 命名与隔离核对表

| 项 | 值 |
|---|---|
| Package Identity | `Microsoft.CmdPal.Ext.Translate` |
| AppExtension Name | `com.microsoft.commandpalette` |
| AppExtension Id | `TranslateCmdPal` |
| COM CLSID | `7F019E71-4A9D-4C06-B035-8272FD1EE90F` |
| CommandProvider.Id | `com.microsoft.cmdpal.translate` |
| Command.Id 前缀 | `com.microsoft.cmdpal.translate.*` |
| SupportedInterfaces | 仅 `Commands` |
| Capabilities | `internetClient`, `runFullTrust` |

## 5. 为什么不并入内置聚合宿主

并入 `Microsoft.CmdPal.Ext.PowerToys.csproj` 的 provider 列表可以省掉新清单/CLSID，但会与 20+ 个内置扩展共用同一进程：任一扩展的未处理异常、设置文件冲突或 SDK 版本漂移都会互相影响。独立包与 Everything 扩展同构，是「不改变 Everything 行为」约束下隔离性最好的选择。
