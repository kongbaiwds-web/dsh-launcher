using System.Diagnostics;
using Microsoft.Win32;

namespace DSHLauncher;

/// <summary>关闭窗口时的行为。</summary>
internal enum CloseBehavior
{
    /// <summary>每次关闭都询问。</summary>
    Ask = 0,

    /// <summary>最小化到系统托盘。</summary>
    MinimizeToTray = 1,

    /// <summary>直接退出。</summary>
    Exit = 2,
}

/// <summary>
/// 设置持久化（HKCU 注册表）+ 开机自启 + 桌面快捷方式。
/// </summary>
internal static class Settings
{
    private const string AppKeyPath = @"Software\DSHLauncher";
    private const string UrlValueName = "Url";
    private const string CloseBehaviorName = "CloseBehavior";
    private const string WinXName = "WinX";
    private const string WinYName = "WinY";
    private const string WinWName = "WinW";
    private const string WinHName = "WinH";

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "DSHLauncher";

    private static readonly string ExecutablePath = Application.ExecutablePath;

    public static string? GetSavedUrl()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(AppKeyPath);
            return key?.GetValue(UrlValueName) as string;
        }
        catch (Exception)
        {
            return null; // 注册表不可读时回退默认地址
        }
    }

    public static void SaveUrl(string url)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(AppKeyPath);
        key.SetValue(UrlValueName, url, RegistryValueKind.String);
    }

    /// <summary>读取关闭方式设置，默认“每次询问”。</summary>
    public static CloseBehavior GetCloseBehavior()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(AppKeyPath);
            if (key?.GetValue(CloseBehaviorName) is int value && Enum.IsDefined(typeof(CloseBehavior), value))
            {
                return (CloseBehavior)value;
            }

            return CloseBehavior.Ask;
        }
        catch (Exception)
        {
            return CloseBehavior.Ask;
        }
    }

    /// <summary>保存关闭方式设置。</summary>
    public static void SetCloseBehavior(CloseBehavior behavior)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(AppKeyPath);
            key.SetValue(CloseBehaviorName, (int)behavior, RegistryValueKind.DWord);
        }
        catch (Exception)
        {
            // 注册表不可写时静默忽略
        }
    }

    /// <summary>读取上次保存的窗口状态（位置 + 大小），没有则返回 null。</summary>
    public static (int X, int Y, int Width, int Height)? GetWindowState()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(AppKeyPath);
            if (key is null)
            {
                return null;
            }

            if (key.GetValue(WinXName) is int x
                && key.GetValue(WinYName) is int y
                && key.GetValue(WinWName) is int w
                && key.GetValue(WinHName) is int h
                && w > 0 && h > 0)
            {
                return (x, y, w, h);
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>保存窗口位置与大小（下次启动自动恢复）。</summary>
    public static void SaveWindowState(int x, int y, int width, int height)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(AppKeyPath);
            key.SetValue(WinXName, x, RegistryValueKind.DWord);
            key.SetValue(WinYName, y, RegistryValueKind.DWord);
            key.SetValue(WinWName, width, RegistryValueKind.DWord);
            key.SetValue(WinHName, height, RegistryValueKind.DWord);
        }
        catch (Exception)
        {
            // 注册表不可写时静默忽略（不影响使用）
        }
    }

    /// <summary>开机自启状态：检查 HKCU Run 键里的值是否指向当前 exe。</summary>
    public static bool IsAutoStartEnabled()
    {
        try
        {
            using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return runKey?.GetValue(RunValueName) is string s
                && s.Contains(ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return false; // 注册表不可读时按未开启处理
        }
    }

    /// <summary>开启/关闭开机自启（写入或删除 HKCU Run 键，无需管理员权限）。</summary>
    public static void SetAutoStartEnabled(bool enabled)
    {
        using RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            runKey.SetValue(RunValueName, $"\"{ExecutablePath}\" --minimized", RegistryValueKind.String);
        }
        else
        {
            runKey.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }

    /// <summary>在用户桌面创建快捷方式，返回快捷方式路径。</summary>
    public static string CreateDesktopShortcut()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string linkPath = Path.Combine(desktop, "DSH 启动器.lnk");

        Type shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("无法创建 WScript.Shell COM 对象。");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(linkPath);
        shortcut.TargetPath = ExecutablePath;
        shortcut.WorkingDirectory = Path.GetDirectoryName(ExecutablePath);
        shortcut.Description = "DeepSeek Harness 桌面启动器（WebView2 套壳）";
        shortcut.IconLocation = $"{ExecutablePath},0";
        shortcut.Save();
        return linkPath;
    }

    public static void OpenInExternalBrowser(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
