# 🐳 DeepSeek Harness 启动器 v1.5.2 发布

> 给 DeepSeek Harness Web GUI 做的 Windows 轻量级桌面启动器，WebView2 套壳，带开机自启、桌面快捷方式、自动拉起 DSH 服务。
> C# / .NET 8 / WinForms，单窗口、常驻系统托盘。

## 🔧 修复

### 真正修复 v1.5.1 未能追加 `--no-open` 的问题

v1.5.1 虽然加入了 `--no-open` 检测，但 `BuildStartCommand` 把默认参数误判成“用户覆盖参数”，导致最终启动命令里没有带上 `--no-open`。

v1.5.2 修复了该判断逻辑：

- 没有注册表覆盖时，会正常走默认命令并自动追加 `--no-open`
- 有注册表覆盖时，也会自动检测并补上 `--no-open`
- 旧版 Harness 不支持 `--no-open` 时，不会传入该参数，保持兼容

### 效果

- 新版 DeepSeek Harness 启动后不会额外弹系统浏览器
- 旧版 Harness 仍可正常启动，不会因未知参数报错

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
| v1.5.1 | 加入 `--no-open` 兼容逻辑（但存在未追加的问题） |
| v1.5.0 | 一键安装程序、检查更新、桌面图标修复 |
| v1.4.0 | DeepSeek Harness 品牌形象（鲸鱼图标）、菜单「关闭 DSH 服务」 |
| v1.3.0 | 启动器自动拉起 DSH 服务 |
