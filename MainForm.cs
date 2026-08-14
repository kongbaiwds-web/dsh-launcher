using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DSHLauncher;

/// <summary>
/// 主窗体：WebView2 套壳 + 工具栏 + 离线横幅 + 系统托盘。
/// </summary>
internal sealed class MainForm : Form
{
    private readonly LaunchOptions _options;
    private readonly EventWaitHandle _showEvent;

    private WebView2 _webView = null!;
    private ToolStrip _toolbar = null!;
    private ToolStripButton _backButton = null!;
    private ToolStripButton _forwardButton = null!;
    private ToolStripButton _refreshButton = null!;
    private ToolStripButton _homeButton = null!;
    private ToolStripTextBox _urlBox = null!;
    private ToolStripDropDownButton _menuButton = null!;
    private ToolStripMenuItem _autoStartItem = null!;
    private ToolStripMenuItem _closeAskItem = null!;
    private ToolStripMenuItem _closeTrayItem = null!;
    private ToolStripMenuItem _closeExitItem = null!;

    private Panel _banner = null!;
    private Label _bannerLabel = null!;

    private NotifyIcon _tray = null!;
    private System.Windows.Forms.Timer _showCheckTimer = null!;
    private System.Windows.Forms.Timer _windowSaveTimer = null!;

    private bool _allowExit;
    private bool _trayTipShown;

    public MainForm(LaunchOptions options, EventWaitHandle showEvent)
    {
        _options = options;
        _showEvent = showEvent;

        Text = "DSH 启动器";
        StartPosition = FormStartPosition.CenterScreen;
        // 首次运行的默认尺寸（用户调整后会被记住并覆盖）
        Size = new Size(1324, 895);
        MinimumSize = new Size(900, 600);
        Icon = AppIcon.Create();
        RestoreWindowState();

        // 添加顺序决定 Dock 布局：先 Fill，再 Top（后添加的优先停靠）
        BuildWebView();
        BuildBanner();
        BuildToolbar();
        BuildTray();
        BuildShowCheckTimer();
        BuildWindowStateSaver();

        Shown += OnShown;
        FormClosing += OnFormClosing;
    }

    private void BuildWebView()
    {
        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
        };
        Controls.Add(_webView);
    }

    private void BuildBanner()
    {
        _banner = new Panel
        {
            Dock = DockStyle.Top,
            Height = 36,
            BackColor = Color.FromArgb(255, 243, 195),
            Visible = false,
        };

        var retryButton = new Button
        {
            Text = "重试",
            Dock = DockStyle.Right,
            Width = 72,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(255, 243, 195),
            Cursor = Cursors.Hand,
        };
        retryButton.Click += (_, _) => _ = EnsureServerAsync();

        _bannerLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Padding = new Padding(10, 0, 0, 0),
            ForeColor = Color.FromArgb(120, 80, 0),
        };

        _banner.Controls.Add(_bannerLabel);
        _banner.Controls.Add(retryButton);
        Controls.Add(_banner);
    }

    private void BuildToolbar()
    {
        _toolbar = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            Padding = new Padding(4, 0, 4, 0),
            AutoSize = false,
            Height = 36,
        };

        _backButton = MakeButton("←", "后退");
        _forwardButton = MakeButton("→", "前进");
        _refreshButton = MakeButton("↻", "刷新");
        _homeButton = MakeButton("⌂", "主页");
        _backButton.Enabled = false;
        _forwardButton.Enabled = false;

        _backButton.Click += (_, _) => _webView.CoreWebView2?.GoBack();
        _forwardButton.Click += (_, _) => _webView.CoreWebView2?.GoForward();
        _refreshButton.Click += (_, _) => _webView.CoreWebView2?.Reload();
        _homeButton.Click += (_, _) => Navigate(_options.Url);

        _urlBox = new ToolStripTextBox
        {
            Width = 460,
            AutoSize = false,
        };
        _urlBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                Navigate(_urlBox.Text);
            }
        };

        var externalButton = new ToolStripButton("外部浏览器")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };
        externalButton.Click += (_, _) => OpenInExternalBrowser();

        _menuButton = new ToolStripDropDownButton("⚙ 菜单")
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
        };

        _autoStartItem = new ToolStripMenuItem("开机自启") { CheckOnClick = true, Checked = Settings.IsAutoStartEnabled() };
        _autoStartItem.CheckedChanged += (_, _) =>
        {
            try
            {
                Settings.SetAutoStartEnabled(_autoStartItem.Checked);
            }
            catch (Exception ex)
            {
                _autoStartItem.Checked = !_autoStartItem.Checked; // 失败回滚勾选状态
                MessageBox.Show(
                    this,
                    "设置开机自启失败：\n" + ex.Message,
                    "DSH 启动器",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };

        var shortcutItem = new ToolStripMenuItem("创建桌面快捷方式");
        shortcutItem.Click += (_, _) => CreateDesktopShortcut();

        var urlItem = new ToolStripMenuItem("修改启动地址…");
        urlItem.Click += (_, _) => ChangeStartUrl();

        var serverItem = new ToolStripMenuItem("启动 DSH 服务");
        serverItem.Click += (_, _) => _ = EnsureServerAsync();

        var closeMenu = new ToolStripMenuItem("关闭方式");
        _closeAskItem = new ToolStripMenuItem("每次询问");
        _closeTrayItem = new ToolStripMenuItem("最小化到托盘");
        _closeExitItem = new ToolStripMenuItem("直接退出");
        _closeAskItem.Click += (_, _) => SetCloseBehaviorMenu(CloseBehavior.Ask);
        _closeTrayItem.Click += (_, _) => SetCloseBehaviorMenu(CloseBehavior.MinimizeToTray);
        _closeExitItem.Click += (_, _) => SetCloseBehaviorMenu(CloseBehavior.Exit);
        closeMenu.DropDownItems.AddRange(new ToolStripItem[] { _closeAskItem, _closeTrayItem, _closeExitItem });
        RefreshCloseMenuChecks();

        var aboutItem = new ToolStripMenuItem("关于");
        aboutItem.Click += (_, _) => ShowAbout();

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) =>
        {
            _allowExit = true;
            Close();
        };

        _menuButton.DropDownItems.AddRange(new ToolStripItem[]
        {
            _autoStartItem,
            shortcutItem,
            urlItem,
            new ToolStripSeparator(),
            serverItem,
            new ToolStripSeparator(),
            closeMenu,
            new ToolStripSeparator(),
            aboutItem,
            exitItem,
        });

        _toolbar.Items.AddRange(new ToolStripItem[]
        {
            _backButton,
            _forwardButton,
            _refreshButton,
            _homeButton,
            new ToolStripSeparator(),
            _urlBox,
            externalButton,
            _menuButton,
        });

        Controls.Add(_toolbar);
    }

    private void BuildTray()
    {
        var trayMenu = new ContextMenuStrip();
        var showItem = new ToolStripMenuItem("显示 DSH 启动器");
        showItem.Click += (_, _) => RestoreFromTray();
        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) =>
        {
            _allowExit = true;
            Close();
        };
        trayMenu.Items.Add(showItem);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(exitItem);

        _tray = new NotifyIcon
        {
            Icon = AppIcon.Create(),
            Text = "DSH 启动器",
            ContextMenuStrip = trayMenu,
            Visible = true,
        };
        _tray.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                RestoreFromTray();
            }
        };
    }

    private void BuildShowCheckTimer()
    {
        // 第二个实例启动时通过命名事件通知本实例显示窗口
        _showCheckTimer = new System.Windows.Forms.Timer { Interval = 400 };
        _showCheckTimer.Tick += (_, _) =>
        {
            if (_showEvent.WaitOne(0))
            {
                RestoreFromTray();
            }
        };
        _showCheckTimer.Start();
    }

    // ---------- 窗口状态记忆 ----------

    private void BuildWindowStateSaver()
    {
        // 拖动/缩放结束后 800ms 防抖保存，崩溃也不丢
        _windowSaveTimer = new System.Windows.Forms.Timer { Interval = 800 };
        _windowSaveTimer.Tick += (_, _) =>
        {
            _windowSaveTimer.Stop();
            SaveWindowStateNow();
        };

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Normal)
            {
                _windowSaveTimer.Stop();
                _windowSaveTimer.Start();
            }
        };
        Move += (_, _) =>
        {
            if (WindowState == FormWindowState.Normal)
            {
                _windowSaveTimer.Stop();
                _windowSaveTimer.Start();
            }
        };
    }

    /// <summary>启动时恢复上次保存的窗口位置与大小。</summary>
    private void RestoreWindowState()
    {
        (int X, int Y, int Width, int Height)? saved = Settings.GetWindowState();
        if (saved is null)
        {
            return;
        }

        (int x, int y, int w, int h) = saved.Value;
        StartPosition = FormStartPosition.Manual;
        Location = new Point(x, y);
        Size = new Size(w, h);

        // 显示器布局变化导致位置跑出屏幕时，回到主屏居中
        var windowBounds = new Rectangle(Location, Size);
        bool visible = false;
        foreach (Screen screen in Screen.AllScreens)
        {
            if (screen.WorkingArea.IntersectsWith(windowBounds))
            {
                visible = true;
                break;
            }
        }

        if (!visible)
        {
            StartPosition = FormStartPosition.CenterScreen;
            Location = new Point(
                (Screen.PrimaryScreen!.WorkingArea.Width - Size.Width) / 2,
                (Screen.PrimaryScreen!.WorkingArea.Height - Size.Height) / 2);
        }
    }

    private void SaveWindowStateNow()
    {
        Rectangle r = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        if (r.Width > 0 && r.Height > 0)
        {
            Settings.SaveWindowState(r.X, r.Y, r.Width, r.Height);
        }
    }

    // ---------- WebView2 ----------

    private async void OnShown(object? sender, EventArgs e)
    {
        await InitializeWebViewAsync();
        if (_options.StartMinimized)
        {
            HideToTray();
        }

        // 服务未运行时自动拉起（开机自启场景的关键：只开窗口不服务 = 之前的“服务暂停”）
        _ = EnsureServerAsync();
    }

    private async Task InitializeWebViewAsync()
    {
        try
        {
            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DSHLauncher", "WebView2");

            CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await _webView.EnsureCoreWebView2Async(env);

            _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            _webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;

            _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            _webView.CoreWebView2.SourceChanged += OnSourceChanged;
            _webView.CoreWebView2.DocumentTitleChanged += OnDocumentTitleChanged;
            _webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;

            Navigate(_options.Url);
        }
        catch (Exception ex)
        {
            // 常见原因：WebView2 运行时未安装
            MessageBox.Show(
                this,
                "WebView2 初始化失败：\n" + ex.Message +
                "\n\n请安装 Microsoft Edge WebView2 运行时：\nhttps://developer.microsoft.com/microsoft-edge/webview2/",
                "DSH 启动器",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            ShowBanner("WebView2 初始化失败，请安装 WebView2 运行时后重试。");
        }
    }

    private void Navigate(string url)
    {
        if (_webView.CoreWebView2 == null)
        {
            return;
        }

        string target = url.Trim();
        if (string.IsNullOrEmpty(target))
        {
            return;
        }

        if (!target.Contains("://", StringComparison.Ordinal))
        {
            target = "http://" + target;
        }

        _webView.CoreWebView2.Navigate(target);
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        // 连接层失败（HTTP 状态码为 0）时显示横幅；HTTP 4xx/5xx 交给页面本身展示
        if (!e.IsSuccess && e.HttpStatusCode == 0)
        {
            ShowBanner($"无法连接到 {_webView.CoreWebView2?.Source}。请确认服务已启动后重试。");
        }
        else
        {
            HideBanner();
        }
    }

    private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (_webView.CoreWebView2 == null)
        {
            return;
        }

        if (!_urlBox.Focused)
        {
            _urlBox.Text = _webView.CoreWebView2.Source;
        }

        _backButton.Enabled = _webView.CoreWebView2.CanGoBack;
        _forwardButton.Enabled = _webView.CoreWebView2.CanGoForward;
    }

    private void OnDocumentTitleChanged(object? sender, object e)
    {
        string? title = _webView.CoreWebView2?.DocumentTitle;
        Text = string.IsNullOrEmpty(title) ? "DSH 启动器" : $"DSH 启动器 — {title}";
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        // 目标为 _blank 的链接一律交给系统默认浏览器
        e.Handled = true;
        Settings.OpenInExternalBrowser(e.Uri);
    }

    // ---------- DSH 服务自愈 ----------

    private bool _serverStarting;

    /// <summary>
    /// 确保 DSH 服务在运行：探测 URL 不可达时自动拉起服务进程并等待就绪，然后重新加载页面。
    /// </summary>
    private async Task EnsureServerAsync()
    {
        if (_serverStarting)
        {
            return;
        }

        _serverStarting = true;
        try
        {
            if (await DshServer.IsReachableAsync(_options.Url))
            {
                return; // 服务已在运行，无需处理
            }

            ShowBanner("DSH 服务未运行，正在自动启动…");
            StartDshServer();

            bool up = await DshServer.WaitUntilReachableAsync(_options.Url, TimeSpan.FromSeconds(120));
            if (up)
            {
                HideBanner();
                Navigate(_options.Url);
            }
            else
            {
                ShowBanner("DSH 服务启动超时，请稍后点击「重试」，或查看日志：%LOCALAPPDATA%\\DSHLauncher\\dsh-server.log");
            }
        }
        catch (Exception ex)
        {
            ShowBanner("启动 DSH 服务出错：" + ex.Message);
        }
        finally
        {
            _serverStarting = false;
        }
    }

    /// <summary>
    /// 以隐藏窗口拉起 DSH 服务进程，输出追加到 %LOCALAPPDATA%\DSHLauncher\dsh-server.log。
    /// 命令等价于 `pnpm dsh web`（工作目录 = DSH 仓库根目录）。
    /// </summary>
    private void StartDshServer()
    {
        (string fileName, string args, string workingDir) = DshServer.BuildStartCommand();

        if (!File.Exists(fileName))
        {
            ShowBanner($"未找到 {fileName}，无法启动 DSH 服务。");
            return;
        }

        if (!Directory.Exists(workingDir))
        {
            ShowBanner($"DSH 服务目录不存在：{workingDir}");
            return;
        }

        string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DSHLauncher");
        Directory.CreateDirectory(logDir);
        string logFile = Path.Combine(logDir, "dsh-server.log");

        try
        {
            File.AppendAllText(
                logFile,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DSHLauncher: 启动服务: {fileName} {args} (cwd={workingDir}){Environment.NewLine}");
        }
        catch (Exception)
        {
            // 日志写不进不阻塞启动
        }

        // 经 cmd.exe 包装以便把输出重定向到日志；进程独立于启动器存活
        string cmdExe = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        string cmdArgs = $"/c \"\"{fileName}\" {args} >> \"{logFile}\" 2>&1\"";

        var psi = new ProcessStartInfo
        {
            FileName = cmdExe,
            Arguments = cmdArgs,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            ShowBanner("启动 DSH 服务失败：" + ex.Message);
        }
    }

    // ---------- 工具栏动作 ----------

    private void OpenInExternalBrowser()
    {
        string url = _webView.CoreWebView2?.Source ?? _options.Url;
        Settings.OpenInExternalBrowser(url);
    }

    private void CreateDesktopShortcut()
    {
        try
        {
            string path = Settings.CreateDesktopShortcut();
            MessageBox.Show(
                this,
                $"桌面快捷方式已创建：\n{path}",
                "DSH 启动器",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                "创建桌面快捷方式失败：\n" + ex.Message,
                "DSH 启动器",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ChangeStartUrl()
    {
        using var dialog = new UrlDialog(_options.Url);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        string url = dialog.Url;
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        _options.Url = url;
        Settings.SaveUrl(url);
        Navigate(url);
    }

    private void ShowAbout()
    {
        Version? version = typeof(Program).Assembly.GetName().Version;
        string webView2Version = CoreWebView2Environment.GetAvailableBrowserVersionString() ?? "未知";
        MessageBox.Show(
            this,
            $"DSH 启动器 v{version}\n\n" +
            $"WebView2 运行时：{webView2Version}\n" +
            $"默认地址：{LaunchOptions.DefaultUrl}\n\n" +
            "DeepSeek Harness 桌面启动器（WebView2 套壳）",
            "关于",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void SetCloseBehaviorMenu(CloseBehavior behavior)
    {
        Settings.SetCloseBehavior(behavior);
        RefreshCloseMenuChecks();
    }

    private void RefreshCloseMenuChecks()
    {
        CloseBehavior behavior = Settings.GetCloseBehavior();
        _closeAskItem.Checked = behavior == CloseBehavior.Ask;
        _closeTrayItem.Checked = behavior == CloseBehavior.MinimizeToTray;
        _closeExitItem.Checked = behavior == CloseBehavior.Exit;
    }

    // ---------- 横幅 ----------

    private void ShowBanner(string message)
    {
        _bannerLabel.Text = message;
        _banner.Visible = true;
    }

    private void HideBanner()
    {
        _banner.Visible = false;
    }

    // ---------- 托盘 ----------

    /// <summary>收进系统托盘（仅“关闭方式=最小化到托盘”或询问时选择托盘时使用）。</summary>
    private void HideToTray()
    {
        // 最小化按钮 = 普通任务栏最小化（不再收进托盘）；仅按“关闭方式”设置收托盘
        ShowInTaskbar = false;
        Hide();
        if (!_trayTipShown)
        {
            _tray.ShowBalloonTip(2500, "DSH 启动器", "已最小化到系统托盘，双击图标可恢复窗口。", ToolTipIcon.Info);
            _trayTipShown = true;
        }
    }

    private void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        // 无论真退出还是收进托盘，都先记住当前窗口状态
        SaveWindowStateNow();

        // 系统关机 / 任务管理器结束进程：直接放行
        if (e.CloseReason is CloseReason.WindowsShutDown or CloseReason.TaskManagerClosing)
        {
            return;
        }

        // 菜单 / 托盘里的显式“退出”：直接放行，不询问
        if (_allowExit)
        {
            return;
        }

        switch (Settings.GetCloseBehavior())
        {
            case CloseBehavior.Exit:
                return; // 直接退出

            case CloseBehavior.MinimizeToTray:
                e.Cancel = true;
                HideToTray();
                return;

            case CloseBehavior.Ask:
            default:
                HandleAskOnClose(e);
                return;
        }
    }

    private void HandleAskOnClose(FormClosingEventArgs e)
    {
        using var dialog = new ClosePromptDialog();
        dialog.ShowDialog(this);

        switch (dialog.Result)
        {
            case ClosePromptDialog.Choice.Exit:
                if (dialog.Remember)
                {
                    Settings.SetCloseBehavior(CloseBehavior.Exit);
                    RefreshCloseMenuChecks();
                }
                return; // 允许关闭

            case ClosePromptDialog.Choice.MinimizeToTray:
                if (dialog.Remember)
                {
                    Settings.SetCloseBehavior(CloseBehavior.MinimizeToTray);
                    RefreshCloseMenuChecks();
                }
                e.Cancel = true;
                HideToTray();
                return;

            case ClosePromptDialog.Choice.None:
            default:
                e.Cancel = true; // 关掉对话框但没选择 → 保持窗口打开
                return;
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _showCheckTimer.Stop();
        _showCheckTimer.Dispose();
        _windowSaveTimer.Stop();
        _windowSaveTimer.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        base.OnFormClosed(e);
    }

    private static ToolStripButton MakeButton(string text, string tooltip)
    {
        return new ToolStripButton(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            ToolTipText = tooltip,
            AutoSize = false,
            Width = 40,
        };
    }
}
