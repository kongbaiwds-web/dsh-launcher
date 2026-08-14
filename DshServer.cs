namespace DSHLauncher;

/// <summary>
/// DSH 服务的启动命令构造与可用性探测。
/// 启动器检测到 DSH 服务未运行时会自动拉起，无需用户先手动开服务。
/// </summary>
internal static class DshServer
{
    /// <summary>
    /// 构造启动命令。优先级：
    /// 1. 注册表覆盖（ServerNode / ServerArgs，如 HKCU\Software\DSHLauncher）；
    /// 2. 用户 PATH 上的 pnpm.cmd（等价于 `pnpm dsh web`）；
    /// 3. 内置兜底：直接用绝对路径的 node 运行 CLI 源入口
    ///    （`pnpm dsh web` 展开后的实际命令，开机自启时 PATH 里往往没有 pnpm）。
    /// </summary>
    public static (string FileName, string Arguments, string WorkingDirectory) BuildStartCommand()
    {
        string serverDir = Settings.GetServerDir();
        string? nodeOverride = Settings.GetServerNode();
        string? argsOverride = Settings.GetServerArgs();

        if (!string.IsNullOrWhiteSpace(nodeOverride) || !string.IsNullOrWhiteSpace(argsOverride))
        {
            string node = string.IsNullOrWhiteSpace(nodeOverride) ? Settings.DefaultServerNode : nodeOverride.Trim();
            string args = string.IsNullOrWhiteSpace(argsOverride) ? Settings.DefaultServerArgs : argsOverride.Trim();
            return (node, args, serverDir);
        }

        string? pnpm = ResolveOnPath("pnpm.cmd");
        if (pnpm is not null)
        {
            return (pnpm, "dsh web", serverDir);
        }

        return (Settings.DefaultServerNode, Settings.DefaultServerArgs, serverDir);
    }

    /// <summary>在 PATH 各目录中查找文件（.cmd/.exe/.bat 等），找不到返回 null。</summary>
    public static string? ResolveOnPath(string fileName)
    {
        string? path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        foreach (string dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(dir.Trim(), fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>快速探测 DSH 服务是否可访问（短超时，任何异常按不可用处理）。</summary>
    public static async Task<bool> IsReachableAsync(string url)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(900) };
            using HttpResponseMessage response = await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>轮询等待服务可访问，直到超时；期间每 1 秒探测一次。</summary>
    public static async Task<bool> WaitUntilReachableAsync(string url, TimeSpan timeout)
    {
        DateTime deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await IsReachableAsync(url))
            {
                return true;
            }

            await Task.Delay(1000);
        }

        return await IsReachableAsync(url);
    }
}
