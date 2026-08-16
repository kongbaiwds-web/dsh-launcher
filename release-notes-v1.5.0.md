# 🐳 DeepSeek Harness启动器 v1.5.0 发布

> 给 DeepSeek Harness Web GUI 做的 Windows 轻量级桌面启动器：**WebView2 套壳**，带**开机自启**、**桌面快捷方式**、**自动拉起 DSH 服务**。C# / .NET 8 / WinForms，单窗口、常驻系统托盘。
>
> **本版亮点**：新增**一键安装程序**（单文件、支持命令行 / AI Agent 静默安装）、⚙ 菜单新增**「检查更新」**，并修复桌面图标空白问题。

![主界面](https://github.com/kongbaiwds-web/dsh-launcher/raw/main/docs/screenshots/v1.5.0-hero.png)

## ✨ 新增

### 一键安装程序（DSHLauncherSetup.exe）

单文件安装程序，内嵌启动器全部文件并带黑色小鲸鱼图标：

- **双击安装**：装到用户目录（`%LOCALAPPDATA%\Programs\DSHLauncher`，无需管理员权限），自动创建桌面快捷方式，可选开机自启，装完自动启动
- **命令行 / AI Agent 静默安装**：支持 `--silent` / `--autostart` / `--launch` / `--dir=路径` 参数，适合脚本与 AI 助手自动执行（退出码 0 = 成功）
- 项目根目录与 GitHub Releases 均提供该安装程序

### ⚙ 菜单新增「检查更新」

![菜单](https://github.com/kongbaiwds-web/dsh-launcher/raw/main/docs/screenshots/v1.5.0-menu-update.png)

- 点击后自动查询 GitHub 上版本号**最高**的 Release（不依赖 GitHub 的 Latest 标记，避免排序坑），与本地版本比较
- 已是最新 → 提示当前版本；有新版本 → 询问**是否立即更新**
- **自动更新**：选择"是"后自动下载最新安装程序 → 静默更新到当前目录 → 自动重启新版（带下载进度条；下载失败会提示手动下载）
- 10 秒超时保护，断网时给出友好提示

## 🚀 改进

- **桌面图标修复**：v1.4.0 图标生成存在矩阵变换 bug，导致鲸鱼被挤出画布、桌面图标显示为空白。已修复为**透明背景的黑色小鲸鱼**（居中、加粗描边、多尺寸适配），并同步修复构建脚本
- **README 完善**：功能列表补充 v1.4.0 新增内容；新增详细「安装」章节（下载安装 / 命令行一键安装 / 可选参数说明）
- **发版脚本升级**：发布时自动构建安装程序并作为 Release 附件上传（安装程序 + 便携版 exe 双附件）

## 🔧 修复

- 图标生成居中 bug（鲸鱼被裁剪到画布右下角 → 桌面图标空白）
- 安装程序解压目录条目兼容（Compress-Archive 反斜杠目录项导致安装失败）
- 设置卡片保存失败问题相关配置说明更新

## 📦 下载与安装

> 安装程序与启动器均为框架依赖版，需要 **.NET 8 Desktop Runtime**（[下载](https://dotnet.microsoft.com/download/dotnet/8.0)）；Win10/11 装 Edge 后一般自带 WebView2。

- **方式一**：下载 `DSHLauncherSetup.exe` 双击安装
- **方式二（命令行 / AI Agent 一键安装）**：

```powershell
irm https://github.com/kongbaiwds-web/dsh-launcher/releases/latest/download/DSHLauncherSetup.exe -OutFile "$env:TEMP\DSHLauncherSetup.exe"; & "$env:TEMP\DSHLauncherSetup.exe" --silent
```

- 便携版：`DSHLauncher.exe`（需 .NET 8 Desktop Runtime）

## 📜 历史版本

| 版本 | 说明 |
| --- | --- |
| v1.4.0 | DeepSeek Harness 品牌形象（鲸鱼图标）、菜单「关闭 DSH 服务」 |
| v1.3.0 | 启动器自动拉起 DSH 服务 |
| v1.2.1 | 最小化改为任务栏最小化 |
