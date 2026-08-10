using System.Drawing;

namespace CrossOff.LobbyDodger;

public sealed class MainForm : Form
{
    private readonly SettingsStore _settingsStore = new();
    private readonly BlacklistService _blacklistService = new();
    private readonly System.Windows.Forms.Timer _monitorTimer = new();
    private readonly System.Windows.Forms.Timer _blacklistTimer = new();

    private readonly Label _statusLabel = new();
    private readonly Label _regionLabel = new();
    private readonly Label _blacklistLabel = new();
    private readonly TextBox _blacklistUrl = new();
    private readonly TextBox _ocrPreview = new();
    private readonly CheckBox _autoDodge = new();
    private readonly NumericUpDown _threshold = new();
    private readonly Button _selectRegion = new();
    private readonly Button _testOcr = new();
    private readonly Button _updateBlacklist = new();
    private readonly Button _startStop = new();

    private AppSettings _settings;
    private OcrService? _ocrService;
    private bool _monitoring;
    private bool _scanInProgress;
    private bool _blacklistUpdateInProgress;
    private string? _lastCandidateKey;
    private int _candidateHits;
    private DateTimeOffset _cooldownUntil;

    public MainForm()
    {
        _settings = _settingsStore.Load();

        Text = "CrossOff Lobby Dodger";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 610);
        ClientSize = new Size(780, 650);
        BackColor = Color.FromArgb(23, 23, 28);
        ForeColor = Color.WhiteSmoke;
        Font = new Font(SystemFonts.MessageBoxFont?.FontFamily ?? FontFamily.GenericSansSerif, 9.5f);

        BuildInterface();
        LoadSettingsIntoInterface();

        _monitorTimer.Interval = _settings.ScanIntervalMs;
        _monitorTimer.Tick += MonitorTimerTick;

        _blacklistTimer.Interval = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
        _blacklistTimer.Tick += BlacklistTimerTick;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        try
        {
            string tessdataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
            _ocrService = new OcrService(tessdataPath);
            SetStatus("Ready. Select the lobby-name area, then test OCR.", StatusKind.Ready);
        }
        catch (Exception exception) when (exception is FileNotFoundException or InvalidOperationException)
        {
            SetStatus(exception.Message, StatusKind.Error);
            _testOcr.Enabled = false;
            _startStop.Enabled = false;
        }

        await RefreshBlacklistAsync();
        _blacklistTimer.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopMonitoring();
        SaveSettingsFromInterface();
        _blacklistTimer.Stop();
        _blacklistService.Dispose();
        _ocrService?.Dispose();
        base.OnFormClosing(e);
    }

    private void BuildInterface()
    {
        var title = new Label
        {
            Text = "CROSS OFF SWF",
            Font = new Font(Font.FontFamily, 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(120, 200, 255),
            AutoSize = true,
            Location = new Point(24, 20)
        };

        var subtitle = new Label
        {
            Text = "Local screen OCR • GitHub blacklist • optional automatic dodge",
            AutoSize = true,
            ForeColor = Color.Silver,
            Location = new Point(27, 59)
        };

        _statusLabel.AutoSize = false;
        _statusLabel.Location = new Point(27, 91);
        _statusLabel.Size = new Size(720, 42);
        _statusLabel.ForeColor = Color.Gainsboro;

        var regionGroup = CreateGroup("1. Capture area", new Rectangle(24, 137, 732, 106));
        _regionLabel.Location = new Point(16, 27);
        _regionLabel.Size = new Size(470, 28);
        _regionLabel.TextAlign = ContentAlignment.MiddleLeft;

        _selectRegion.Text = "Select area";
        _selectRegion.Location = new Point(504, 25);
        _selectRegion.Size = new Size(100, 31);
        _selectRegion.Click += SelectRegionClick;

        _testOcr.Text = "Test OCR";
        _testOcr.Location = new Point(612, 25);
        _testOcr.Size = new Size(96, 31);
        _testOcr.Click += TestOcrClick;

        var regionHelp = new Label
        {
            Text = "Draw tightly around the survivor/player names. Exclude icons and unrelated UI where possible.",
            ForeColor = Color.Silver,
            AutoSize = true,
            Location = new Point(17, 64)
        };
        regionGroup.Controls.AddRange([_regionLabel, _selectRegion, _testOcr, regionHelp]);

        var blacklistGroup = CreateGroup("2. GitHub blacklist", new Rectangle(24, 254, 732, 118));
        _blacklistUrl.Location = new Point(16, 27);
        _blacklistUrl.Size = new Size(585, 27);
        _blacklistUrl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _updateBlacklist.Text = "Update";
        _updateBlacklist.Location = new Point(612, 25);
        _updateBlacklist.Size = new Size(96, 31);
        _updateBlacklist.Click += async (_, _) => await RefreshBlacklistAsync();

        _blacklistLabel.Location = new Point(17, 65);
        _blacklistLabel.Size = new Size(691, 32);
        _blacklistLabel.ForeColor = Color.Silver;
        blacklistGroup.Controls.AddRange([_blacklistUrl, _updateBlacklist, _blacklistLabel]);

        var settingsGroup = CreateGroup("3. Behavior", new Rectangle(24, 383, 732, 92));
        _autoDodge.Text = "Automatically press Esc after a confirmed match";
        _autoDodge.AutoSize = true;
        _autoDodge.Location = new Point(17, 31);
        _autoDodge.CheckedChanged += (_, _) => SaveSettingsFromInterface();

        var thresholdLabel = new Label
        {
            Text = "OCR brightness threshold:",
            AutoSize = true,
            Location = new Point(493, 32)
        };
        _threshold.Minimum = 50;
        _threshold.Maximum = 240;
        _threshold.Location = new Point(647, 28);
        _threshold.Size = new Size(61, 27);
        settingsGroup.Controls.AddRange([_autoDodge, thresholdLabel, _threshold]);

        var previewLabel = new Label
        {
            Text = "Last OCR preview (not saved):",
            AutoSize = true,
            Location = new Point(27, 490),
            ForeColor = Color.Silver
        };

        _ocrPreview.Location = new Point(27, 515);
        _ocrPreview.Size = new Size(729, 75);
        _ocrPreview.Multiline = true;
        _ocrPreview.ReadOnly = true;
        _ocrPreview.ScrollBars = ScrollBars.Vertical;
        _ocrPreview.BackColor = Color.FromArgb(14, 14, 17);
        _ocrPreview.ForeColor = Color.Gainsboro;
        _ocrPreview.BorderStyle = BorderStyle.FixedSingle;

        _startStop.Text = "Start monitoring";
        _startStop.Font = new Font(Font.FontFamily, 11, FontStyle.Bold);
        _startStop.Location = new Point(552, 603);
        _startStop.Size = new Size(204, 38);
        _startStop.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _startStop.Click += StartStopClick;

        var localOnly = new Label
        {
            Text = "No screenshots or recognized names are uploaded.",
            AutoSize = true,
            ForeColor = Color.FromArgb(130, 210, 150),
            Location = new Point(27, 614),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        Controls.AddRange([
            title,
            subtitle,
            _statusLabel,
            regionGroup,
            blacklistGroup,
            settingsGroup,
            previewLabel,
            _ocrPreview,
            _startStop,
            localOnly
        ]);
    }

    private static GroupBox CreateGroup(string text, Rectangle bounds)
    {
        return new GroupBox
        {
            Text = text,
            Bounds = bounds,
            ForeColor = Color.Gainsboro,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
    }

    private void LoadSettingsIntoInterface()
    {
        _blacklistUrl.Text = _settings.BlacklistUrl;
        _autoDodge.Checked = _settings.AutoDodge;
        _threshold.Value = Math.Clamp(_settings.OcrThreshold, (int)_threshold.Minimum, (int)_threshold.Maximum);
        UpdateRegionLabel();
    }

    private void SaveSettingsFromInterface()
    {
        _settings.BlacklistUrl = _blacklistUrl.Text.Trim();
        _settings.AutoDodge = _autoDodge.Checked;
        _settings.OcrThreshold = (int)_threshold.Value;

        try
        {
            _settingsStore.Save(_settings);
        }
        catch (IOException exception)
        {
            SetStatus($"Could not save settings: {exception.Message}", StatusKind.Error);
        }
    }

    private void SelectRegionClick(object? sender, EventArgs e)
    {
        bool restartAfterSelection = _monitoring;
        StopMonitoring();
        Hide();

        using var selector = new RegionSelectorForm(_settings.CaptureRegion);
        DialogResult result = selector.ShowDialog();

        Show();
        Activate();

        if (result == DialogResult.OK)
        {
            _settings.CaptureRegion = selector.SelectedScreenRegion;
            SaveSettingsFromInterface();
            UpdateRegionLabel();
            SetStatus("Capture area saved. Use Test OCR while the lobby is visible.", StatusKind.Ready);
        }

        if (restartAfterSelection && _settings.HasCaptureRegion)
        {
            StartMonitoring();
        }
    }

    private async void TestOcrClick(object? sender, EventArgs e)
    {
        if (!_settings.HasCaptureRegion || _ocrService is null)
        {
            MessageBox.Show(
                "Select the lobby-name area first.",
                "CrossOff Lobby Dodger",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (_scanInProgress)
        {
            return;
        }

        SaveSettingsFromInterface();
        _scanInProgress = true;
        _testOcr.Enabled = false;

        try
        {
            using Bitmap screenshot = ScreenCapture.Capture(_settings.CaptureRegion);
            OcrScan scan = await _ocrService.RecognizeAsync(screenshot, _settings.OcrThreshold);
            _ocrPreview.Text = string.IsNullOrWhiteSpace(scan.Text) ? "(No text recognized)" : scan.Text;
            NameMatch? match = NameMatcher.FindMatch(scan.Text, _blacklistService.Current.Entries);
            string matchText = match is null ? "no blacklist match" : $"matched {match.Alias}";
            SetStatus($"OCR confidence {scan.MeanConfidence:P0}; {matchText}.", StatusKind.Ready);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            SetStatus($"OCR test failed: {exception.Message}", StatusKind.Error);
        }
        finally
        {
            _scanInProgress = false;
            _testOcr.Enabled = true;
        }
    }

    private async Task RefreshBlacklistAsync()
    {
        if (_blacklistUpdateInProgress)
        {
            return;
        }

        SaveSettingsFromInterface();
        _blacklistUpdateInProgress = true;
        _updateBlacklist.Enabled = false;
        _blacklistLabel.Text = "Updating…";

        try
        {
            BlacklistUpdateResult result = await _blacklistService.RefreshAsync(_settings.BlacklistUrl);
            int active = result.Document.Entries.Count(static entry => entry.Active);
            string source = result.UsedCache ? "cached copy" : "GitHub";
            _blacklistLabel.Text = $"{active} active entries • updated {result.Document.UpdatedAt:u} • {source}";

            if (result.Error is not null)
            {
                string fallback = result.UsedCache ? "using the cached list" : "no usable list is loaded";
                SetStatus($"Blacklist update failed; {fallback}. {result.Error}", StatusKind.Warning);
            }
        }
        finally
        {
            _blacklistUpdateInProgress = false;
            _updateBlacklist.Enabled = true;
        }
    }

    private void StartStopClick(object? sender, EventArgs e)
    {
        if (_monitoring)
        {
            StopMonitoring();
            return;
        }

        StartMonitoring();
    }

    private void StartMonitoring()
    {
        if (_ocrService is null)
        {
            SetStatus("OCR is unavailable. Re-extract the complete release ZIP.", StatusKind.Error);
            return;
        }

        if (!_settings.HasCaptureRegion)
        {
            MessageBox.Show(
                "Select the lobby-name area before starting.",
                "CrossOff Lobby Dodger",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        SaveSettingsFromInterface();
        _monitoring = true;
        _candidateHits = 0;
        _lastCandidateKey = null;
        _monitorTimer.Interval = Math.Clamp(_settings.ScanIntervalMs, 500, 10_000);
        _monitorTimer.Start();
        _startStop.Text = "Stop monitoring";
        _startStop.BackColor = Color.FromArgb(115, 42, 48);
        SetStatus(
            _settings.AutoDodge
                ? "Monitoring. Confirmed matches will warn and press Esc when DBD is foreground."
                : "Monitoring in manual mode. Confirmed matches will warn only.",
            StatusKind.Monitoring);
        WindowState = FormWindowState.Minimized;
    }

    private void StopMonitoring()
    {
        _monitorTimer.Stop();
        _monitoring = false;
        _candidateHits = 0;
        _lastCandidateKey = null;
        _startStop.Text = "Start monitoring";
        _startStop.UseVisualStyleBackColor = true;
        if (!IsDisposed)
        {
            SetStatus("Monitoring stopped.", StatusKind.Ready);
        }
    }

    private async void MonitorTimerTick(object? sender, EventArgs e)
    {
        if (!_monitoring || _scanInProgress || _ocrService is null || DateTimeOffset.UtcNow < _cooldownUntil)
        {
            return;
        }

        _scanInProgress = true;

        try
        {
            using Bitmap screenshot = ScreenCapture.Capture(_settings.CaptureRegion);
            OcrScan scan = await _ocrService.RecognizeAsync(screenshot, _settings.OcrThreshold);
            _ocrPreview.Text = string.IsNullOrWhiteSpace(scan.Text) ? "(No text recognized)" : scan.Text;
            NameMatch? match = NameMatcher.FindMatch(scan.Text, _blacklistService.Current.Entries);

            if (match is null)
            {
                _candidateHits = 0;
                _lastCandidateKey = null;
                return;
            }

            string key = $"{match.Entry.Id}\u001f{match.Alias}";
            if (key.Equals(_lastCandidateKey, StringComparison.Ordinal))
            {
                _candidateHits++;
            }
            else
            {
                _lastCandidateKey = key;
                _candidateHits = 1;
            }

            if (_candidateHits < Math.Clamp(_settings.RequiredConsecutiveMatches, 2, 5))
            {
                SetStatus($"Possible match: {match.Alias}. Confirming on the next scan…", StatusKind.Warning);
                return;
            }

            TriggerMatch(match);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            SetStatus($"Screen scan failed: {exception.Message}", StatusKind.Error);
        }
        finally
        {
            _scanInProgress = false;
        }
    }

    private void TriggerMatch(NameMatch match)
    {
        _cooldownUntil = DateTimeOffset.UtcNow.AddSeconds(
            Math.Clamp(_settings.MatchCooldownSeconds, 15, 300));
        _candidateHits = 0;
        _lastCandidateKey = null;

        string actionText;
        if (!_settings.AutoDodge)
        {
            actionText = "Manual mode: no key was pressed.";
        }
        else if (!InputService.ForegroundLooksLikeDeadByDaylight())
        {
            actionText = "Automatic dodge skipped: Dead by Daylight was not the foreground window.";
        }
        else if (InputService.SendEscape())
        {
            actionText = "Escape was sent to the foreground Dead by Daylight window.";
        }
        else
        {
            actionText = "Windows rejected the Escape input; dodge manually.";
        }

        SetStatus($"Blacklist match: {match.Alias} ({match.Entry.Group}).", StatusKind.Warning);
        var alert = new AlertForm(match, actionText);
        alert.FormClosed += (_, _) => alert.Dispose();
        alert.Show();
    }

    private async void BlacklistTimerTick(object? sender, EventArgs e)
    {
        await RefreshBlacklistAsync();
    }

    private void UpdateRegionLabel()
    {
        Rectangle region = _settings.CaptureRegion;
        _regionLabel.Text = _settings.HasCaptureRegion
            ? $"X {region.X}, Y {region.Y} • {region.Width} × {region.Height} pixels"
            : "No capture area selected";
    }

    private void SetStatus(string message, StatusKind kind)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = kind switch
        {
            StatusKind.Ready => Color.Gainsboro,
            StatusKind.Monitoring => Color.FromArgb(130, 220, 155),
            StatusKind.Warning => Color.FromArgb(255, 210, 90),
            StatusKind.Error => Color.FromArgb(255, 100, 100),
            _ => Color.Gainsboro
        };
    }

    private enum StatusKind
    {
        Ready,
        Monitoring,
        Warning,
        Error
    }
}
