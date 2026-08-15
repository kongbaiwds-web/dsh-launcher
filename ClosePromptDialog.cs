namespace DSHLauncher;

/// <summary>关闭窗口时的选择对话框：直接关闭 / 最小化到托盘 + 以后不再提示。</summary>
internal sealed class ClosePromptDialog : Form
{
    public enum Choice
    {
        None,
        Exit,
        MinimizeToTray,
    }

    private readonly CheckBox _rememberBox;

    public ClosePromptDialog()
    {
        Text = "DeepSeek Harness启动器";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(400, 156);

        var label = new Label
        {
            Text = "关闭窗口时希望怎样处理？",
            Location = new Point(16, 16),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 10f, FontStyle.Bold),
        };

        var hint = new Label
        {
            Text = "（也可以在 ⚙ 菜单 → 关闭方式 中修改）",
            Location = new Point(16, 40),
            AutoSize = true,
            ForeColor = Color.Gray,
        };

        var exitButton = new Button
        {
            Text = "直接关闭",
            Location = new Point(110, 66),
            Size = new Size(120, 32),
        };
        exitButton.Click += (_, _) => Finish(Choice.Exit);

        var trayButton = new Button
        {
            Text = "最小化到托盘",
            Location = new Point(242, 66),
            Size = new Size(138, 32),
        };
        trayButton.Click += (_, _) => Finish(Choice.MinimizeToTray);

        _rememberBox = new CheckBox
        {
            Text = "以后不再提示",
            Location = new Point(16, 112),
            AutoSize = true,
        };

        AcceptButton = exitButton;

        // ESC 或关闭对话框 = 不选择，保持窗口打开
        var cancel = new Button { Visible = false, DialogResult = DialogResult.Cancel };
        CancelButton = cancel;

        Controls.Add(label);
        Controls.Add(hint);
        Controls.Add(exitButton);
        Controls.Add(trayButton);
        Controls.Add(_rememberBox);
        Controls.Add(cancel);
    }

    public Choice Result { get; private set; } = Choice.None;

    /// <summary>是否勾选了“以后不再提示”。</summary>
    public bool Remember => _rememberBox.Checked;

    private void Finish(Choice choice)
    {
        Result = choice;
        DialogResult = DialogResult.OK;
    }
}
