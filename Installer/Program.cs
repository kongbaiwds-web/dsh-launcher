using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using Microsoft.Win32;

namespace DSHLauncherInstaller;

/// <summary>
/// DeepSeek Harness启动器 一键安装程序（单文件，内嵌启动器全部文件）。
/// 交互模式：图形界面，可选手动安装。
/// 静默模式：--silent（可选 --autostart / --launch / --dir=路径），供命令行与 AI Agent 调用，
/// 成功退出码 0，失败非 0。
/// </summary>
internal static class Program
{
    private const string AppName = "DeepSeek Harness启动器";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "DSHLauncher";
    private const string ShortcutName = "DeepSeek Harness启动器.lnk";

    [STAThread]
    private static void Main()
    {
        string[] args = Environment.GetCommandLineArgs();
        if (HasFlag(args, "--silent"))
        {
            Environment.Exit(SilentInstall(args));
            return;
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerForm());
    }

    private static bool HasFlag(string[] args, string flag)
        => Array.Exists(args, a => string.Equals(a, flag, StringComparison.OrdinalIgnoreCase));

    private static int SilentInstall(string[] args)
    {
        string dir = DefaultInstallDir();
        foreach (string a in args)
        {
            if (a.StartsWith("--dir=", StringComparison.OrdinalIgnoreCase) && a.Length > 6)
            {
                dir = a[6..].Trim().Trim('"');
            }
        }
        bool autoStart = HasFlag(args, "--autostart");
        bool launch = HasFlag(args, "--launch");
        string? error = PerformInstall(dir, autoStart, launch, interactive: false);
        return error == null ? 0 : 1;
    }

    /// <summary>核心安装逻辑：解压、快捷方式、可选自启与启动。返回错误信息；成功返回 null。</summary>
    private static string? PerformInstall(string dir, bool autoStart, bool launch, bool interactive)
    {
        if (!Path.IsPathRooted(dir)) return "安装目录必须是绝对路径。";
        string exe = Path.Combine(dir, "DSHLauncher.exe");
        if (File.Exists(exe) && IsRunning("DSHLauncher"))
        {
            // 自动更新场景：旧启动器已启动本安装程序并即将退出，等待其退出（最多约 6 秒）
            bool clear = false;
            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(300);
                if (!IsRunning("DSHLauncher")) { clear = true; break; }
            }
            if (!clear) return "检测到启动器正在运行，请先关闭它再继续安装。";
        }

        try
        {
            Directory.CreateDirectory(dir);
            using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip");
            if (stream == null) return "安装程序缺少内置文件 payload.zip";
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry entry in zip.Entries)
            {
                // 统一用 "/" 分隔（Compress-Archive 可能写入 "\\"）；以 "/" 结尾的是目录条目，跳过
                string name = entry.FullName.Replace('\\', '/');
                if (name.EndsWith("/", StringComparison.Ordinal)) continue;
                string target = Path.Combine(dir, name);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target, overwrite: true);
            }

            try { CreateDesktopShortcut(exe, dir); }
            catch { /* 快捷方式失败不阻塞安装 */ }

            if (autoStart)
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
                key.SetValue(RunValueName, $"\"{exe}\" --minimized", RegistryValueKind.String);
            }

            if (launch)
            {
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = dir });
            }
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static string DefaultInstallDir()
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "Programs", "DSHLauncher");
    }

    private static bool IsRunning(string processName)
    {
        try { return Process.GetProcessesByName(processName).Length > 0; }
        catch { return false; }
    }

    private static void CreateDesktopShortcut(string targetExe, string workingDir)
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string linkPath = Path.Combine(desktop, ShortcutName);
        Type shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("无法创建 WScript.Shell COM 对象。");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(linkPath);
        shortcut.TargetPath = targetExe;
        shortcut.WorkingDirectory = workingDir;
        shortcut.IconLocation = $"{targetExe},0";
        shortcut.Description = AppName;
        shortcut.Save();
    }

    private sealed class InstallerForm : Form
    {
        private readonly TextBox _dirBox;
        private readonly CheckBox _autoStartBox;
        private readonly Button _installButton;
        private readonly Label _statusLabel;

        public InstallerForm()
        {
            Text = $"{AppName} 安装程序";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(470, 320);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { /* 忽略图标加载失败 */ }

            var title = new Label
            {
                Text = $"🐳 {AppName} 一键安装",
                Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold),
                Location = new Point(22, 18),
                AutoSize = true,
            };

            var desc = new Label
            {
                Text = "将安装到用户目录（无需管理员权限），自动创建桌面快捷方式。",
                Location = new Point(24, 52),
                AutoSize = true,
                ForeColor = Color.DimGray,
            };

            var dirLabel = new Label { Text = "安装目录：", Location = new Point(24, 92), AutoSize = true };
            _dirBox = new TextBox
            {
                Location = new Point(110, 88),
                Width = 250,
                Text = DefaultInstallDir(),
            };
            var browseButton = new Button
            {
                Text = "浏览…",
                Location = new Point(368, 86),
                Width = 78,
                Height = 26,
            };
            browseButton.Click += (_, _) =>
            {
                using var dialog = new FolderBrowserDialog { Description = "选择安装目录", SelectedPath = _dirBox.Text };
                if (dialog.ShowDialog(this) == DialogResult.OK) _dirBox.Text = dialog.SelectedPath;
            };

            _autoStartBox = new CheckBox
            {
                Text = "开机自动启动（驻留托盘，自动拉起 DSH 服务）",
                Location = new Point(26, 128),
                AutoSize = true,
                Checked = false,
            };

            _statusLabel = new Label
            {
                Text = "",
                Location = new Point(24, 168),
                AutoSize = true,
                ForeColor = Color.SeaGreen,
            };

            _installButton = new Button
            {
                Text = "安装",
                Location = new Point(268, 250),
                Width = 88,
                Height = 32,
                BackColor = Color.FromArgb(30, 144, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            _installButton.Click += Install_Click;

            var cancelButton = new Button
            {
                Text = "取消",
                Location = new Point(362, 250),
                Width = 88,
                Height = 32,
            };
            cancelButton.Click += (_, _) => Close();

            Controls.AddRange(new Control[]
            {
                title, desc, dirLabel, _dirBox, browseButton, _autoStartBox, _statusLabel, _installButton, cancelButton,
            });
        }

        private async void Install_Click(object? sender, EventArgs e)
        {
            string dir;
            try { dir = Path.GetFullPath(_dirBox.Text.Trim()); }
            catch { MessageBox.Show(this, "安装目录无效。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!Path.IsPathRooted(dir))
            {
                MessageBox.Show(this, "安装目录必须是绝对路径。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _installButton.Enabled = false;
            _statusLabel.Text = "正在安装…";

            string? error = await Task.Run(() => PerformInstall(dir, _autoStartBox.Checked, launch: true, interactive: true));
            if (error == null)
            {
                _statusLabel.Text = "安装完成";
                MessageBox.Show(this, $"安装完成！\n\n已安装到：{dir}\n桌面已创建快捷方式" + (_autoStartBox.Checked ? "，已开启开机自启。" : "。"),
                    AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Close();
            }
            else
            {
                _statusLabel.Text = "安装失败";
                MessageBox.Show(this, "安装失败：" + error, AppName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                _installButton.Enabled = true;
            }
        }
    }
}
