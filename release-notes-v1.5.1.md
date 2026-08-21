# 🐳 DeepSeek Harness 启动器 v1.5.1 发布

> 给 DeepSeek Harness Web GUI 做的 Windows 轻量级桌面启动器，WebView2 套壳，带开机自启、桌面快捷方式、自动拉起 DSH 服务。
> C# / .NET 8 / WinForms，单窗口、常驻系统托盘。

## 🔧 修复

### 适配新版 DeepSeek Harness 自动打开浏览器

新版 Harness 的 `dsh web` 默认会在服务就绪后调用系统默认浏览器打开 Web UI，导致启动器自己的 WebView2 窗口之外又多开一个浏览器。

本次更新后，启动器会在拉起服务时自动传入：

```text
--no-open
```

新版 Harness 不会再额外弹浏览器，界面仍正常显示在启动器窗口内。

### 兼容旧版 Harness

启动器会自动检测 Harness 是否支持 `--no-open`：

- 新版：自动追加 `--no-open`
- 旧版：不传该参数，保持原启动方式，避免因未知参数导致启动失败

## 📥 下载与安装

> 安装程序与启动器均为框架依赖版，需要 **.NET 8 Desktop Runtime**；Win10/11 装 Edge 后一般自带 WebView2。

- **方式一**：下载 `DSHLauncherSetup.exe` 双击安装
- **方式二（命令行 / AI Agent 一键安装）**：

```powershell
irm https://github.com/kongbaiwds-web/dsh-launcher/releases/latest/download/DSHLauncherSetup.exe -OutFile "$env:TEMP\DSHLauncherSetup.exe"; & "$env:TEMP\DSHLauncherSetup.exe" --silent
```

- 便携版：`DSHLauncher.exe`（需 .NET 8 Desktop Runtime）

## 📜 历史版本

| 版本 | 说明 |
| --- | --- |
| v1.5.0 | 一键安装程序、检查更新、桌面图标修复 |
| v1.4.0 | DeepSeek Harness 品牌形象（鲸鱼图标）、菜单「关闭 DSH 服务」 |
| v1.3.0 | 启动器自动拉起 DSH 服务 |
| v1.2.1 | 最小化改为任务栏最小化 |
