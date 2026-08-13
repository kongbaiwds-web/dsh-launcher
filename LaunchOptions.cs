namespace DSHLauncher;

/// <summary>命令行启动选项。</summary>
internal sealed class LaunchOptions
{
    public const string DefaultUrl = "http://127.0.0.1:3080";

    public string Url { get; internal set; } = DefaultUrl;

    /// <summary>启动后直接最小化到托盘（开机自启时使用）。</summary>
    public bool StartMinimized { get; internal set; }

    public static LaunchOptions Parse(string[] args)
    {
        var options = new LaunchOptions();
        string? urlArg = null;

        foreach (string arg in args)
        {
            if (arg.StartsWith("--url=", StringComparison.OrdinalIgnoreCase))
            {
                urlArg = arg["--url=".Length..].Trim();
            }
            else if (arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase))
            {
                options.StartMinimized = true;
            }
        }

        // 优先级：命令行参数 > 已保存设置 > 默认地址
        if (!string.IsNullOrEmpty(urlArg))
        {
            options.Url = urlArg;
        }
        else
        {
            string? saved = Settings.GetSavedUrl();
            if (!string.IsNullOrEmpty(saved))
            {
                options.Url = saved;
            }
        }

        return options;
    }
}
