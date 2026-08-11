using System.Diagnostics;
using System.Drawing;

namespace CrossOff.LobbyDodger;

public enum AlertDecision
{
    Dodge,
    Cancelled
}

public sealed class AlertForm : Form
{
    private readonly bool _automatic;
    private readonly int _countdownSeconds;
    private readonly Label _action = new();
    private readonly Label _countdown = new();
    private readonly Button _cancel = new();
    private readonly Button _close = new();
    private readonly System.Windows.Forms.Timer _countdownTimer = new();
    private readonly TaskCompletionSource<AlertDecision> _decision = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private DateTimeOffset _dodgeAt;

    protected override bool ShowWithoutActivation => true;

    public AlertForm(NameMatch match, bool automatic, int countdownSeconds)
    {
        _automatic = automatic;
        _countdownSeconds = Math.Clamp(countdownSeconds, 1, 10);

        Text = "DBD Ranked cross-off lobby dodger — match detected";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        TopMost = true;
        ClientSize = new Size(540, 354);
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;
        Font = AppTheme.UiFont();
        Padding = new Padding(24, 20, 24, 18);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = AppTheme.Background,
            Margin = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var heading = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = AppTheme.Background,
            Margin = new Padding(0, 0, 0, 4)
        };
        heading.Controls.Add(new PictureBox
        {
            Image = AppGlyphs.Create(AppGlyph.Warning, AppTheme.Warning, 24),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Size = new Size(28, 28),
            Margin = new Padding(0, 0, 8, 0)
        });
        heading.Controls.Add(new Label
        {
            Text = "BLACKLIST MATCH DETECTED",
            Font = AppTheme.UiFont(16, FontStyle.Bold),
            ForeColor = AppTheme.Warning,
            AutoSize = true,
            Margin = new Padding(0, 1, 0, 0)
        });
        root.Controls.Add(heading, 0, 0);

        root.Controls.Add(new Label
        {
            Text = automatic ? "The lobby will be left unless you cancel." : "Leave this lobby manually.",
            AutoSize = true,
            ForeColor = AppTheme.MutedText,
            Margin = new Padding(0, 0, 0, 16)
        }, 0, 1);

        var details = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle,
            Padding = new Padding(15, 13, 15, 13),
            Margin = new Padding(0)
        };
        details.Controls.Add(new Label
        {
            Text = $"{match.Alias}\r\n\r\nGroup: {match.Entry.Group}\r\nReason: {match.Entry.Reason}",
            Font = AppTheme.UiFont(10),
            ForeColor = AppTheme.Text,
            AutoSize = false,
            Dock = DockStyle.Fill
        });
        root.Controls.Add(details, 0, 2);

        _countdown.AutoSize = true;
        _countdown.ForeColor = AppTheme.Warning;
        _countdown.Font = AppTheme.UiFont(10, FontStyle.Bold);
        _countdown.Margin = new Padding(0, 13, 0, 4);
        _countdown.Visible = automatic;
        root.Controls.Add(_countdown, 0, 3);

        _action.AutoSize = true;
        _action.ForeColor = AppTheme.MutedText;
        _action.Margin = new Padding(0, 4, 0, 10);
        _action.Text = automatic
            ? "Dead by Daylight must remain the foreground window."
            : "No keyboard input will be sent.";
        root.Controls.Add(_action, 0, 4);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = AppTheme.Background
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var evidence = new LinkLabel
        {
            Text = "Open reviewed evidence  ↗",
            LinkColor = AppTheme.Emerald,
            ActiveLinkColor = AppTheme.Text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0)
        };
        evidence.LinkClicked += (_, _) => OpenUrl(match.Entry.EvidenceUrl);

        _cancel.Text = "Cancel auto-dodge";
        _cancel.Size = new Size(142, 34);
        _cancel.Margin = new Padding(8, 0, 0, 0);
        _cancel.Visible = automatic;
        AppTheme.StyleButton(_cancel, glyph: AppGlyph.Cancel);
        _cancel.Click += (_, _) => CancelDodge();

        _close.Text = automatic ? "Dismiss" : "Close";
        _close.Size = new Size(88, 34);
        _close.Margin = new Padding(8, 0, 0, 0);
        AppTheme.StyleButton(_close, primary: !automatic);
        _close.Click += (_, _) => Close();

        footer.Controls.Add(evidence, 0, 0);
        footer.Controls.Add(_cancel, 1, 0);
        footer.Controls.Add(_close, 2, 0);
        root.Controls.Add(footer, 0, 5);
        Controls.Add(root);

        _countdownTimer.Interval = 100;
        _countdownTimer.Tick += CountdownTimerTick;
    }

    public Task<AlertDecision> WaitForDecisionAsync() => _decision.Task;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        if (_automatic)
        {
            _dodgeAt = DateTimeOffset.UtcNow.AddSeconds(_countdownSeconds);
            UpdateCountdown();
            _countdownTimer.Start();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _countdownTimer.Stop();
        if (_automatic)
        {
            _decision.TrySetResult(AlertDecision.Cancelled);
        }

        base.OnFormClosing(e);
    }

    public void UpdateActionText(string actionText)
    {
        if (IsDisposed)
        {
            return;
        }

        _countdownTimer.Stop();
        _countdown.Visible = false;
        _action.Text = actionText;
        _action.ForeColor = actionText.StartsWith("Lobby leave confirmed", StringComparison.Ordinal)
            ? AppTheme.Emerald
            : AppTheme.Warning;
        _cancel.Visible = false;
        _close.Text = "Close";
        AppTheme.StyleButton(_close, primary: true);
    }

    private void CountdownTimerTick(object? sender, EventArgs e)
    {
        UpdateCountdown();
    }

    private void UpdateCountdown()
    {
        TimeSpan remaining = _dodgeAt - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            _countdownTimer.Stop();
            _countdown.Text = "Leaving lobby now…";
            _cancel.Enabled = false;
            _decision.TrySetResult(AlertDecision.Dodge);
            return;
        }

        _countdown.Text = $"Leaving lobby in {remaining.TotalSeconds:0.0} seconds…";
    }

    private void CancelDodge()
    {
        _countdownTimer.Stop();
        _decision.TrySetResult(AlertDecision.Cancelled);
        _countdown.Visible = false;
        _action.Text = "Automatic dodge cancelled. Leave manually if needed.";
        _action.ForeColor = AppTheme.Warning;
        _cancel.Visible = false;
        _close.Text = "Close";
        AppTheme.StyleButton(_close, primary: true);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            MessageBox.Show(
                $"Windows could not open the evidence link.\r\n\r\n{url}",
                "DBD Ranked cross-off lobby dodger",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
