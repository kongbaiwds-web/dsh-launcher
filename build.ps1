# Build script: publish Release to publish\win-x64
# Usage: pwsh ./build.ps1
# If system dotnet is not in PATH, use E:\deepseek\work\.dotnet

$ErrorActionPreference = "Stop"

$dotnet = "dotnet"
$localDotnet = "E:\deepseek\work\.dotnet\dotnet.exe"
if (Test-Path $localDotnet) {
    $dotnet = $localDotnet
}

$outDir = Join-Path $PSScriptRoot "publish\win-x64"

# Generate DeepSeek Harness whale icon and overwrite DSHLauncher.ico
$iconGenerator = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

public static class DeepSeekIconGenerator
{
    private const string WhalePath =
        "M48.8354 10.0479C48.3232 9.79199 48.1025 10.2798 47.8032 10.5278C47.7007 10.6079 47.6143 10.7119 47.5273 10.8076C46.7793 11.624 45.9048 12.1597 44.7622 12.0957C43.0923 12 41.666 12.5356 40.4058 13.8398C40.1377 12.2319 39.2476 11.272 37.8926 10.6558C37.1836 10.3359 36.4668 10.0156 35.9702 9.31982C35.6235 8.82373 35.5293 8.27197 35.356 7.72754C35.2456 7.3999 35.1353 7.06396 34.7651 7.00781C34.3633 6.94385 34.2056 7.2876 34.0479 7.57568C33.418 8.75195 33.1733 10.0479 33.1973 11.3599C33.2524 14.312 34.4736 16.6641 36.8999 18.3359C37.1758 18.5278 37.2466 18.7197 37.1597 19C36.9946 19.5757 36.7974 20.1357 36.624 20.7119C36.5137 21.0801 36.3486 21.1597 35.9624 21C34.6309 20.4321 33.481 19.5918 32.4644 18.5757C30.7393 16.8721 29.1792 14.9917 27.2334 13.52C26.7764 13.1758 26.3193 12.856 25.8467 12.5518C23.8618 10.584 26.1069 8.96777 26.627 8.77588C27.1704 8.57568 26.8159 7.8877 25.0591 7.896C23.3022 7.90381 21.6953 8.50391 19.647 9.30371C19.3477 9.42383 19.0322 9.51172 18.7095 9.58398C16.8501 9.22363 14.9199 9.14355 12.9033 9.37598C9.10596 9.80762 6.07275 11.6396 3.84326 14.7681C1.16455 18.5278 0.53418 22.7998 1.30664 27.2559C2.11768 31.9521 4.46582 35.8398 8.07373 38.8799C11.8159 42.0322 16.1255 43.5762 21.041 43.2803C24.0269 43.104 27.3516 42.6963 31.1016 39.4561C32.0469 39.936 33.0396 40.1279 34.686 40.272C35.9546 40.3921 37.1758 40.208 38.1211 40.0078C39.6021 39.688 39.4995 38.2881 38.9639 38.0322C34.623 35.9678 35.5762 36.8081 34.71 36.1279C36.9155 33.4639 40.2402 30.6958 41.54 21.728C41.6426 21.0161 41.5557 20.5679 41.54 19.9917C41.5322 19.6396 41.6108 19.5039 42.0049 19.4639C43.0923 19.3359 44.1479 19.0317 45.1167 18.4878C47.9292 16.9199 49.064 14.3438 49.3315 11.2559C49.3711 10.7837 49.3237 10.2959 48.8354 10.0479ZM24.3262 37.8398C20.1196 34.4639 18.0791 33.3521 17.2358 33.3999C16.4482 33.4482 16.5898 34.3682 16.7632 34.9678C16.9443 35.5601 17.1812 35.9683 17.5117 36.4878C17.7402 36.832 17.8979 37.3442 17.2832 37.728C15.9282 38.584 13.5728 37.4399 13.4624 37.3838C10.7207 35.7358 8.42822 33.5601 6.81348 30.584C5.25342 27.7197 4.34766 24.6479 4.19775 21.3677C4.1582 20.5757 4.38672 20.2959 5.15869 20.1519C6.17529 19.96 7.22314 19.9199 8.23926 20.0718C12.5327 20.7119 16.1885 22.6719 19.2529 25.7759C21.002 27.5439 22.3252 29.6558 23.6885 31.7202C25.1377 33.9121 26.6978 36 28.6831 37.7119C29.3843 38.312 29.9434 38.7681 30.479 39.104C28.8643 39.2881 26.1699 39.3281 24.3262 37.8398ZM26.3433 24.6001C26.3433 24.248 26.6191 23.9678 26.9658 23.9678C27.0444 23.9678 27.1152 23.9839 27.1782 24.0078C27.2651 24.04 27.3438 24.0879 27.4067 24.1602C27.5171 24.272 27.5801 24.4321 27.5801 24.6001C27.5801 24.9521 27.3042 25.2319 26.9575 25.2319C26.6108 25.2319 26.3433 24.9521 26.3433 24.6001ZM32.6064 27.8799C32.2046 28.0479 31.8027 28.1919 31.4165 28.208C30.8179 28.2397 30.1641 27.9922 29.8096 27.688C29.2583 27.2158 28.8643 26.9521 28.6987 26.1279C28.6279 25.7759 28.6675 25.2319 28.7305 24.9199C28.8721 24.248 28.7144 23.8159 28.2495 23.4238C27.8716 23.104 27.3911 23.0161 26.8633 23.0161C26.666 23.0161 26.4849 22.9277 26.3511 22.856C26.1304 22.7441 25.9492 22.4639 26.1226 22.1201C26.1777 22.0078 26.4458 21.7358 26.5088 21.688C27.2256 21.272 28.0527 21.4077 28.8169 21.7197C29.5259 22.0161 30.0615 22.5601 30.834 23.3281C31.6216 24.2559 31.7632 24.5117 32.2124 25.208C32.5669 25.752 32.8901 26.312 33.1104 26.9521C33.2446 27.3521 33.0713 27.6802 32.6064 27.8799Z";

    public static void Generate(string outputPath)
    {
        using (var bitmap = new Bitmap(256, 256))
        {
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var path = BuildWhalePath())
                using (var brush = new SolidBrush(Color.Black))
                using (var pen = new Pen(Color.Black, 10f) { LineJoin = LineJoin.Round, StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }
            }

            IntPtr hIcon = bitmap.GetHicon();
            try
            {
                using (var icon = Icon.FromHandle(hIcon))
                using (var stream = File.Create(outputPath))
                {
                    icon.Save(stream);
                }
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }
    }

    private static GraphicsPath BuildWhalePath()
    {
        var path = new GraphicsPath();
        ParseSvgPath(WhalePath, path);
        RectangleF bounds = path.GetBounds();
        const float targetSize = 224f;
        float scale = Math.Min(targetSize / bounds.Width, targetSize / bounds.Height);
        float cx = (256f - bounds.Width * scale) / 2f;
        float cy = (256f - bounds.Height * scale) / 2f;
        // single explicit matrix (no composition-order ambiguity -> whale stays centered)
        using (var matrix = new Matrix(scale, 0f, 0f, scale, cx - bounds.X * scale, cy - bounds.Y * scale))
        {
            path.Transform(matrix);
        }
        return path;
    }

    private static void ParseSvgPath(string d, GraphicsPath path)
    {
        List<string> tokens = Tokenize(d);
        int index = 0;
        float x = 0f, y = 0f;
        float startX = 0f, startY = 0f;
        char command = '\0';

        while (index < tokens.Count)
        {
            string token = tokens[index];
            if (token.Length == 1 && char.IsLetter(token[0]))
            {
                command = token[0];
                index++;
            }

            switch (command)
            {
                case 'M':
                    float mx = ReadNumber(tokens, ref index);
                    float my = ReadNumber(tokens, ref index);
                    x = mx;
                    y = my;
                    startX = mx;
                    startY = my;
                    path.StartFigure();
                    break;

                case 'C':
                    float x1 = ReadNumber(tokens, ref index);
                    float y1 = ReadNumber(tokens, ref index);
                    float x2 = ReadNumber(tokens, ref index);
                    float y2 = ReadNumber(tokens, ref index);
                    float cx = ReadNumber(tokens, ref index);
                    float cy = ReadNumber(tokens, ref index);
                    path.AddBezier(x, y, x1, y1, x2, y2, cx, cy);
                    x = cx;
                    y = cy;
                    break;

                case 'Z':
                    path.CloseFigure();
                    x = startX;
                    y = startY;
                    break;
            }
        }
    }

    private static List<string> Tokenize(string d)
    {
        var tokens = new List<string>();
        int i = 0;
        while (i < d.Length)
        {
            char c = d[i];
            if (char.IsWhiteSpace(c) || c == ',')
            {
                i++;
                continue;
            }

            if (char.IsLetter(c))
            {
                tokens.Add(c.ToString());
                i++;
                continue;
            }

            int start = i;
            while (i < d.Length && (char.IsDigit(d[i]) || d[i] == '.' || d[i] == '-' || d[i] == '+' || d[i] == 'e' || d[i] == 'E'))
            {
                i++;
            }

            if (i == start)
            {
                i++;
            }
            else
            {
                tokens.Add(d.Substring(start, i - start));
            }
        }

        return tokens;
    }

    private static float ReadNumber(List<string> tokens, ref int index)
    {
        return float.Parse(tokens[index++], CultureInfo.InvariantCulture);
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
'@

$drawingAssembly = "System.Drawing"
try { Add-Type -AssemblyName $drawingAssembly -ErrorAction Stop } catch { $drawingAssembly = "System.Drawing.Common"; Add-Type -AssemblyName $drawingAssembly -ErrorAction Stop }
Add-Type -TypeDefinition $iconGenerator -ReferencedAssemblies @($drawingAssembly)
$iconPath = Join-Path $PSScriptRoot "DSHLauncher.ico"
[DeepSeekIconGenerator]::Generate($iconPath)
Write-Host "Generated DeepSeek Harness icon: $iconPath"

# Stop old launcher if running so publish can overwrite output files
Stop-Process -Name DSHLauncher -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500


& $dotnet publish (Join-Path $PSScriptRoot "DSHLauncher.csproj") `
    -c Release -r win-x64 --self-contained false -o $outDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Update desktop shortcut: remove old name, create new DeepSeek Harness launcher shortcut
$desktopDir = [Environment]::GetFolderPath('Desktop')
$oldShortcutName = "DSH " + [char]0x542F + [char]0x52A8 + [char]0x5668 + ".lnk"
$oldShortcutPath = Join-Path $desktopDir $oldShortcutName
$newShortcutName = "DeepSeek Harness" + [char]0x542F + [char]0x52A8 + [char]0x5668 + ".lnk"
$newShortcutPath = Join-Path $desktopDir $newShortcutName
if (Test-Path $oldShortcutPath) { Remove-Item $oldShortcutPath -Force }
$exePath = Join-Path $outDir "DSHLauncher.exe"
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($newShortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $outDir
$shortcut.IconLocation = "$exePath,0"
$shortcut.Description = "DeepSeek Harness launcher"
$shortcut.Save()
Write-Host "Desktop shortcut updated: $newShortcutPath"

Write-Host ""
Write-Host "Build complete. Output: $outDir" -ForegroundColor Green
Write-Host "Tip: use the exe from the publish directory, not bin\Debug."
