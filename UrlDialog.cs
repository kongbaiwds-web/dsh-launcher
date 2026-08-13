namespace DSHLauncher;

/// <summary>修改启动地址的小对话框。</summary>
internal sealed class UrlDialog : Form
{
    private readonly TextBox _urlBox;

    public UrlDialog(string currentUrl)
    {
        Text = "修改启动地址";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 112);

        var label = new Label
        {
            Text = "启动地址 (URL)：",
            Location = new Point(12, 14),
            AutoSize = true,
        };

        _urlBox = new TextBox
        {
            Location = new Point(12, 38),
            Width = 396,
            Text = currentUrl,
        };

        var ok = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            Location = new Point(232, 70),
            Size = new Size(80, 28),
        };
        var cancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(328, 70),
            Size = new Size(80, 28),
        };

        AcceptButton = ok;
        CancelButton = cancel;

        Controls.Add(label);
        Controls.Add(_urlBox);
        Controls.Add(ok);
        Controls.Add(cancel);
    }

    public string Url => _urlBox.Text.Trim();
}
