using System.Threading;

namespace DSHLauncher;

internal static class Program
{
    private const string MutexName = @"Local\DSHLauncher.SingleInstance";
    private const string ShowEventName = @"Local\DSHLauncher.ShowEvent";

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // 单实例：重复启动时通知已有实例显示窗口，然后退出
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            SignalRunningInstance();
            return;
        }

        using var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        LaunchOptions options = LaunchOptions.Parse(args);
        using var mainForm = new MainForm(options, showEvent);
        Application.Run(mainForm);
    }

    private static void SignalRunningInstance()
    {
        try
        {
            using var handle = EventWaitHandle.OpenExisting(ShowEventName);
            handle.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            // 首个实例已退出，忽略
        }
    }
}
