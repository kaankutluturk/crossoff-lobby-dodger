using System.Diagnostics;
using System.Drawing;

namespace CrossOff.LobbyDodger;

public sealed class AlertForm : Form
{
    public AlertForm(NameMatch match, string actionText)
    {
        Text = "CrossOff Lobby Dodger — match detected";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        TopMost = true;
        ClientSize = new Size(520, 300);
        BackColor = Color.FromArgb(18, 18, 22);
        ForeColor = Color.White;

        var title = new Label
        {
            Text = "BLACKLIST MATCH DETECTED",
            Font = new Font(SystemFonts.MessageBoxFont?.FontFamily ?? FontFamily.GenericSansSerif, 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 92, 92),
            AutoSize = true,
            Location = new Point(24, 22)
        };

        var details = new Label
        {
            Text = $"Alias: {match.Alias}\r\nGroup: {match.Entry.Group}\r\nReason: {match.Entry.Reason}",
            Font = new Font(SystemFonts.MessageBoxFont?.FontFamily ?? FontFamily.GenericSansSerif, 10),
            AutoSize = false,
            Location = new Point(26, 72),
            Size = new Size(468, 105)
        };

        var action = new Label
        {
            Text = actionText,
            ForeColor = Color.FromArgb(255, 210, 90),
            AutoSize = false,
            Location = new Point(26, 181),
            Size = new Size(468, 35)
        };

        var evidence = new LinkLabel
        {
            Text = "Open reviewed evidence",
            LinkColor = Color.DeepSkyBlue,
            ActiveLinkColor = Color.White,
            AutoSize = true,
            Location = new Point(26, 229)
        };
        evidence.LinkClicked += (_, _) => OpenUrl(match.Entry.EvidenceUrl);

        var close = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Location = new Point(401, 252),
            Size = new Size(92, 30)
        };

        AcceptButton = close;
        Controls.AddRange([title, details, action, evidence, close]);
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
                "CrossOff Lobby Dodger",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
