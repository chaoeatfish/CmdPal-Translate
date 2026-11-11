# 验证要点

## A. 工程与静态检查（本机可完成）

| 项 | 命令 | 判定 |
|---|---|---|
| 核心逻辑单测 | `dotnet test tests/TranslateCmdPal.Core.UnitTests/TranslateCmdPal.Core.UnitTests.csproj` | 全部通过（前缀解析、语种判定、缓存、取消、失败、交换方向） |
| 静态一致性 | `python tools/verify.py` | CLSID 三处一致、XML/JSON 合法、CPM 版本齐全、Core 无 WinRT 依赖、resx 与 Designer 键一致 |
| 仓库集成预演 | `python repo-integration/integrate.py --repo <PowerToys 根> --dry-run` | 输出待追加的 4 个清单改动，无意外文件 |
| UI 层编译 | 按官方 API 表面做桩编译（临时校验工程） | 0 错误 0 警告 |

**本机结果（2026-11-11，安装版）**：单测 19/19 通过、静态校验全部通过、真实 SDK `0.9.260303001` 编译主工程 0 错误 0 警告、`makeappx pack` + `signtool sign` 成功；集成脚本预演与实跑均正确。首次安装报 `0x800B0109` → 用户授权导入根证书/开启旁加载后安装成功（`0.0.3.0 Status Ok`）。**但命令面板激活扩展失败**：日志 `Failed to find 翻译: -2147024891 (0x80070005)` + `You forgot to start the extension`。已排除构建产物、清单属性、部署方式（MSIX/松散注册）、进程竞态、exe 自身、开发者许可证等因素；Everything 扩展始终正常加载。判定为 Windows 10 对自签名侧载包 packaged COM 激活的平台限制。

## B. 构建与部署（需 .NET 10 + Windows SDK 26100 的开发机）

| 项 | 判定 |
|---|---|
| x64 构建 | `dotnet build -c Debug -p:Platform=x64`（.NET 10 SDK）成功；`dotnet publish -p:PublishProfile=win-x64.pubxml` 产出可打包布局 |
| 部署 | VS `Build > Deploy` 或 `Add-AppxPackage -Register AppxManifest.xml` 成功 |
| 扩展发现 | `Reload Command Palette extensions` 后，「设置 → 扩展」列表同时出现 Everything 与「翻译」 |
| 稀疏包检查 | `src/modules/cmdpal/check-extensions.ps1` 在本扩展目录下能找到 `Appx/Microsoft.CommandPalette.Extensions.winmd` |
| SDK 版本 | 扩展引用的 winmd 与宿主 CmdPal 版本一致；版本错配会 `E_NOINTERFACE` 且扩展不出现在列表 |

## C. Everything 回归（核心约束）

| 项 | 判定 |
|---|---|
| 结果不变 | 同一查询（含无前缀查询）下 Everything 的结果数、排序、图标、路径显示一致 |
| 内联无污染 | 输入 `report.pdf`、`合同` 等无前缀文本时，首页不出现本扩展的 fallback 条目 |
| 命令栏不变 | Everything 的打开 / 复制路径 / 浏览 / Show more 等命令可用，右下命令栏顺序不变 |
| 设置不变 | Max、Sort、Query Prefix、Match Path、RegEx、Run as user、Enable Send to 保持原值；`filters.toml` 与 ECP 包目录无写入 |
| 包不变 | Everything 扩展的 `AppxManifest.xml`、CLSID、版本、安装路径未被修改 |

## D. 功能验证

| 项 | 判定 |
|---|---|
| 触发 | `tr hello`、`翻译 你好` 出现内联译文；`hello` 不出现；`tr` 空文本不发请求 |
| 方向 | 中文→英文、英文→中文自动生效；`tr >en 你好` 与 `tr hello >zh` 覆盖目标；交换方向命令重译为对侧 |
| 展示 | Title=译文、Subtitle=方向·引擎·耗时；`Enter` 复制译文并出现 toast；长文详情页 Markdown 正常渲染 |
| 命令栏 | 复制原文 / 交换方向 / 详情 / 翻译剪贴板 / 导入密钥 / 设置均可用 |
| 密钥 | 导入后 `translate.settings.json` 中不含密钥；日志文件不含密钥；重启扩展后仍可用 |
| 异常 | 断网、超时、401、429、空响应 → 显示降级条目，宿主不崩溃、不卡顿 |
| 性能 | 连续输入 10 次仅 1 次网络请求（防抖 + 取消）；相同文本二次查询命中缓存 |
| 本地化 | `Invoke-PseudoLocalization.ps1` 后界面无截断；`format_sources.ps1` 通过 |
| 生命周期 | 卸载扩展后设置与凭据文件可清理；升级不覆盖 Everything 设置 |
