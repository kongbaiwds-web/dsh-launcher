# DSH 启动器（DSHLauncher）

给 DeepSeek Harness Web GUI 做的 Windows 轻量级桌面启动器：**WebView2 套壳**，带
**开机自启**和**桌面快捷方式**支持。C# / .NET 8 / WinForms，单窗口、常驻系统托盘。

![技术栈](https://img.shields.io/badge/.NET-8.0-512BD4) ![WebView2](https://img.shields.io/badge/WebView2-Evergreen-0078d6)

## 功能

- **关闭方式可选**：⚙ 菜单 →「关闭方式」：每次询问 / 最小化到托盘 / 直接退出；「每次询问」时点关闭会弹选择框，可勾选「以后不再提示」记住本次选择
- **记住窗口状态**：窗口位置和大小自动记忆（拖动/缩放后 800ms 防抖写入注册表，关闭时再存一次），下次启动原样恢复；首次运行默认 1324×895
- **WebView2 套壳**：默认加载 `http://127.0.0.1:3080`（DeepSeek Harness Web GUI）
- **开机自启**：⚙ 菜单一键开启/关闭（写入 `HKCU\...\CurrentVersion\Run`，无需管理员权限）；
  自启时以 `--minimized` 启动，直接驻留托盘
- **桌面快捷方式**：⚙ 菜单一键创建「DSH 启动器.lnk」到桌面
- **系统托盘**：关窗/最小化不退出，驻留托盘；双击托盘图标恢复；首次提示气泡
- **离线横幅**：服务未启动时显示黄色横幅 + 重试按钮，不弹错误页干瞪眼
- **工具栏**：后退 / 前进 / 刷新 / 主页 / 地址栏（回车跳转）/ 外部浏览器
- **单实例**：重复启动会唤起已有窗口（而不是再开一个）
- **地址可改**：⚙ 菜单「修改启动地址…」，设置保存在注册表，也可用 `--url=` 临时指定

## 构建

需要 .NET 8 SDK（本机若没有，参考文末「本机 SDK 安装」）。

```powershell
pwsh ./build.ps1
# 或手动：
dotnet publish DSHLauncher.csproj -c Release -r win-x64 --self-contained false -o publish\win-x64
```

产物在 `publish\win-x64\DSHLauncher.exe`（框架依赖，需要 .NET 8 Desktop Runtime；
WebView2 运行时需已安装，Win10/11 装 Edge 后一般自带）。

## 使用

1. 先启动 DSH 服务（确保 `http://127.0.0.1:3080` 可访问）
2. 双击 `DSHLauncher.exe`
3. 点右上角 **⚙ 菜单**：
   - **开机自启**：勾选后开机自动驻留托盘
   - **创建桌面快捷方式**：桌面上生成「DSH 启动器.lnk」
   - **修改启动地址…**：改默认 URL
4. 关闭窗口 = 最小化到托盘；托盘右键 → **退出** 才真正退出

### 命令行参数

| 参数 | 说明 |
|---|---|
| `--minimized` | 启动后直接最小化到托盘（开机自启自动携带） |
| `--url=<地址>` | 临时指定启动地址（优先级高于注册表保存值） |

## 实现细节

| 事项 | 做法 |
|---|---|
| 开机自启 | `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 写入 `"<exe>" --minimized`；取消即删值。检测逻辑校验值指向当前 exe |
| 桌面快捷方式 | `WScript.Shell` COM 创建 `.lnk`，目标为当前 exe（自动适配 OneDrive 重定向的桌面） |
| 设置持久化 | `HKCU\Software\DSHLauncher\Url` |
| WebView2 数据目录 | `%LOCALAPPDATA%\DSHLauncher\WebView2`（exe 可随意移动，不产生垃圾） |
| 单实例 | 命名 Mutex + 命名 EventWaitHandle，二次启动 Set 事件让首实例弹窗 |
| 外部链接 | `_blank` 弹窗一律交给系统默认浏览器 |

> ⚠️ 开机自启和快捷方式都绑定**当前 exe 路径**：若移动/重命名 exe，需重新勾选自启、
> 重新创建快捷方式。

## 常见问题

- **横幅提示「无法连接到…」**：DSH 服务没启动，启动服务后点「重试」即可
- **提示 WebView2 初始化失败**：安装 [WebView2 运行时](https://developer.microsoft.com/microsoft-edge/webview2/)
- **目标机没有 .NET 8 Desktop Runtime**：先装
  [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)，
  或把发布命令改成 `--self-contained true` 得到免运行时版本（体积约 +70MB）

## 本机 SDK 安装备注（无管理员、无 winget 可用时）

沙箱环境里 schannel TLS 不可用，winget / Invoke-WebRequest 都会失败，可用
Python（OpenSSL）下载 SDK zip 后手动解压：

```powershell
# 1) 从 https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json
#    找到最新稳定版 SDK 的 win-x64 zip 地址，用 python 下载
# 2) 解压到任意目录（本机为 E:\deepseek\work\.dotnet）
# 3) 使用：& "E:\deepseek\work\.dotnet\dotnet.exe" ...  或加入 PATH
```

## 目录结构

```
dsh-launcher/
├── DSHLauncher.csproj   # 项目文件（net8.0-windows + WebView2 包）
├── Program.cs           # 入口：单实例 + 事件唤醒
├── LaunchOptions.cs     # 命令行参数解析
├── Settings.cs          # 注册表设置 / 开机自启 / 桌面快捷方式
├── MainForm.cs          # 主窗体：工具栏、横幅、托盘、WebView2 逻辑
├── UrlDialog.cs         # 修改地址对话框
├── AppIcon.cs           # 运行时绘制的应用图标
├── build.ps1            # 一键发布脚本
└── publish/win-x64/     # 构建产物
```
