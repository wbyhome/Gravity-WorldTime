# Gravity WorldTime

**把世界时钟磁吸在桌面，让联系多一点体贴。**

将城市时钟组合成组、拖到屏幕边缘收起，用朋友视角的提示了解什么时候再联系。

[下载 0.1.11](https://github.com/wbyhome/Gravity-WorldTime/releases/tag/v0.1.11) · [English](README.md)

![凌晨时钟示例](media/clock-example-zh-CN.png)

*真实 WPF 界面渲染，采用洛杉矶凌晨 04:26 的固定示例时间。提示是一般估计，不是对方的真实状态。*

## 功能

- **磁吸分组**：整组移动，靠近其他组松手即可吸附。
- **从头部拆分**：拖下方时钟左侧头部，移动本条及其以下部分。
- **边缘自动收起**：拖到屏幕工作区边缘松手，折叠为小标签，点击展开。
- **桌面驻留或置顶**：选择原生桌面显示，或显示在普通窗口上方。
- **419 个城市和地点、247 个国家/地区**，支持四种语言离线搜索。
- **朋友视角的两行提示**：可能的生活情境，以及联系建议或对方当地时间。
- **背景半透明、文字清晰**，可调整不透明度和提示字号。
- **系统托盘入口**，不为每座城市创建任务栏按钮。

这是独立的 Windows 原型程序，与 Gravity Desktop 主应用分开运行。

## 下载和启动

1. 前往 [Releases](https://github.com/wbyhome/Gravity-WorldTime/releases/tag/v0.1.11) 下载 Windows x64 ZIP。
2. 完整解压到可写入目录，保留 EXE 和配套文件。
3. 如未安装，请安装 **.NET 8 Desktop Runtime（x64）**。普通 .NET Runtime 不包含 WPF。
4. 打开 `Gravity.WorldTime.exe`，点击系统托盘图标管理城市和设置。

目标环境为 Windows 10/11 x64 交互式桌面，未对所有 Windows 版本完整认证。这是依赖桌面运行时的便携原型，不是安装程序或 Microsoft Store 包。首次运行创建示例时钟；下载包不包含个人布局。

## 添加城市

点击托盘图标 → **添加城市**。可以先按国家/地区筛选，也可以搜索英语、简体中文、繁体中文或日语名称。支持 `US`、`USA`、`UK` 等代码和常用缩写，支持组合搜索 `United States New York`，也可以用不带重音的 `Sao Paulo` 查找 São Paulo。

![添加城市界面](media/city-picker-zh-CN.png)

*隔离环境中的真实 WPF 界面渲染。目录可以离线使用。*

多时区国家可选择不同地点。目录不是全球所有城市的全集；未列出的地点可以直接选择对应 Windows 时区，再修改时钟显示名称。

名称与映射来自 **Unicode CLDR 48.2**，当前时差和夏令时由本机 Windows 时区数据库计算。247 的统计包含国家、属地和地区，不等于 247 个主权国家。[目录出处](docs/catalog-provenance.json) · [第三方声明](THIRD_PARTY_NOTICES.md)

## 移动、分组和折叠

| 操作 | 效果 |
| --- | --- |
| 拖最上面的时钟 | 移动整组 |
| 拖下方时钟左侧头部 | 拆出本条及其以下所有时钟并移动 |
| 拖下方时钟正文 | 不拆分分组 |
| 靠近可组合的其他组松手 | 启用磁吸时组合成组 |
| 拖到屏幕工作区边缘松手 | 自动折叠 |
| 按住 **Alt** 松手 | 取消本次磁吸和自动折叠 |
| 点击边缘标签 | 展开对应分组 |
| 右键时钟或标签 | 打开可用操作菜单 |

也可手动折叠或“展开全部时钟”。折叠状态会在重启后保留；展开后不会立刻再次折叠。原显示器缺失时，边缘标签回到可用屏幕，时钟顺序和保存布局保留。

## 外观和显示模式

**桌面驻留**通过原生透明小窗口挂载到 Explorer 桌面宿主，WPF 负责生成时钟画面。**固定最上层**使用 WPF 浮动窗口。切换模式时重建显示窗口，保留城市和分组。

**恢复时钟（切换置顶）**会展开时钟，把分组放回工作区内并置顶，可能调整越界位置。

默认背景不透明度 **35%**，即透明度 65%，文字保持不透明。设置支持 15–100% 背景不透明度、提示字号、12/24 小时制、磁吸、生活提示、联系建议、通常工作/联系时段和一小时休息时间。

英语为基础语言。支持英语、简体中文、繁体中文、日语，默认跟随系统，不支持的系统语言回退英语，也可手动选择。

## 像朋友一样提醒

时钟区分凌晨、早上、下午、傍晚、夜晚和深夜。凌晨 04:26 会提醒朋友可能正睡得香；吃饭、忙碌、下班路上、晚间放松各有不同文案。第二行直接给出联系建议，或根据已设作息显示明确的 **对方当地时间**。

这些只是一般估计。程序不知道对方真实活动、睡眠、节假日或日历，不提供在线状态检测，也不需要账号或 API 密钥。生活提示和联系建议可以独立关闭。

## 数据和更新

配置保存在 EXE 旁的 `data/worldtime.json`，保存时保留一代 `.bak`；可用 `--data-dir` 指定其他目录。

更新程序文件前先退出，保留原有 `data` 文件夹；发行 ZIP 不包含该目录。移除时钟城市不会删除桌面文件。

## 构建和检查

使用能够构建 .NET 8 目标的 Windows x64 .NET SDK：

```powershell
dotnet build .\src\WorldTime.csproj -c Release
.\src\bin\Release\net8.0-windows\Gravity.WorldTime.exe
```

隔离的模型与目录检查：

```powershell
New-Item -ItemType Directory -Force .\artifacts | Out-Null
Start-Process .\src\bin\Release\net8.0-windows\Gravity.WorldTime.exe -ArgumentList '--self-test --report artifacts/self-test.txt' -Wait
Get-Content .\artifacts\self-test.txt
```

`--ui-test`、`--layer-test`、`--layered-child-test` 同样接受 `--report <路径>`。界面/原生检查创建临时测试窗口，需要交互式 Windows 桌面。

编译后的目录已随源码提供。如需重新生成，在 Windows 上使用 **PowerShell 7**，依次运行 `tools/Fetch-CatalogSources.ps1`、`tools/Build-CityCatalog.ps1`。下载脚本固定 CLDR release-48-2 并核对来源哈希。

## 原型状态

桌面驻留显示已在本地确认。实际 Win+D、真实鼠标操作的自动折叠/拆分/组合、混合 DPI 多显示器、Explorer 重启恢复仍需分别验收，没有全部标记通过。

自动绘制和界面检查是有限证据，不保证所有桌面配置均正常。[验证说明](docs/VALIDATION.md)

可在 [Issues](https://github.com/wbyhome/Gravity-WorldTime/issues) 提供版本、Windows 版本、显示缩放和复现步骤。不要无意公开私人布局或桌面截图。

## 源码与许可

Copyright © 2026 Gravity Desktop. All rights reserved unless otherwise specified.

此次公开原型源码，不额外授予开源许可证。Unicode 数据保留独立许可。[源码与许可](SOURCE_AND_LICENSING.md) · [第三方声明](THIRD_PARTY_NOTICES.md)
