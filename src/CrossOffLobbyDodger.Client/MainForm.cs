using System.Drawing;

namespace CrossOff.LobbyDodger;

public sealed class MainForm : Form
{
    private const int RequiredConsecutiveMatches = 2;

    private readonly SettingsStore _settingsStore = new();
    private readonly BlacklistService _blacklistService = new();
    private readonly System.Windows.Forms.Timer _monitorTimer = new();
    private readonly System.Windows.Forms.Timer _blacklistTimer = new();

    private readonly Label _statusLabel = new();
    private readonly Label _statusPill = new();
    private readonly Label _regionLabel = new();
    private readonly Label _blacklistLabel = new();
    private readonly TextBox _ocrPreview = new();
    private readonly RadioButton _warnOnly = new();
    private readonly RadioButton _warnAndDodge = new();
    private readonly Label _behaviorSequence = new();
    private readonly Button _selectRegion = new();
    private readonly Button _testOcr = new();
    private readonly Button _updateBlacklist = new();
    private readonly Button _startStop = new();
    private readonly TableLayoutPanel _setupView = new();
    private readonly TableLayoutPanel _monitoringView = new();
    private readonly Label _monitoringLastScan = new();

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

        Text = "DBD Ranked cross-off lobby dodger";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 610);
        ClientSize = new Size(800, 650);
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;
        Font = AppTheme.UiFont();
        AutoScaleMode = AutoScaleMode.Dpi;

        try
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch (ArgumentException)
        {
            // The project icon is still supplied through the application manifest.
        }

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
        Padding = new Padding(24, 20, 24, 18);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = AppTheme.Background,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildHeader(), 0, 0);

        _statusLabel.AutoSize = true;
        _statusLabel.ForeColor = AppTheme.MutedText;
        _statusLabel.Margin = new Padding(1, 13, 0, 15);
        root.Controls.Add(_statusLabel, 0, 1);

        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Background,
            Margin = new Padding(0)
        };
        BuildSetupView();
        BuildMonitoringView();
        contentHost.Controls.Add(_monitoringView);
        contentHost.Controls.Add(_setupView);
        _setupView.BringToFront();
        root.Controls.Add(contentHost, 0, 2);

        root.Controls.Add(BuildFooter(), 0, 3);
        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = AppTheme.Background
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var brand = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            BackColor = AppTheme.Background
        };

        if (Icon is not null)
        {
            brand.Controls.Add(new PictureBox
            {
                Image = Icon.ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(48, 48),
                Margin = new Padding(0, 0, 13, 0)
            });
        }

        var copy = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = AppTheme.Background
        };
        copy.Controls.Add(new Label
        {
            Text = "LOBBY DODGER",
            AutoSize = true,
            Font = AppTheme.UiFont(18, FontStyle.Bold),
            ForeColor = AppTheme.Text,
            Margin = new Padding(0, 1, 0, 1)
        });
        copy.Controls.Add(new Label
        {
            Text = "Local OCR for DBD Ranked cross-off lobbies",
            AutoSize = true,
            ForeColor = AppTheme.MutedText,
            Margin = new Padding(0)
        });
        brand.Controls.Add(copy);

        _statusPill.AutoSize = true;
        _statusPill.Text = "●  Ready";
        _statusPill.TextAlign = ContentAlignment.MiddleCenter;
        _statusPill.ForeColor = AppTheme.Emerald;
        _statusPill.BackColor = AppTheme.EmeraldSurface;
        _statusPill.Padding = new Padding(10, 6, 10, 6);
        _statusPill.Margin = new Padding(12, 8, 0, 0);

        header.Controls.Add(brand, 0, 0);
        header.Controls.Add(_statusPill, 1, 0);
        return header;
    }

    private void BuildSetupView()
    {
        _setupView.Dock = DockStyle.Fill;
        _setupView.AutoScroll = true;
        _setupView.ColumnCount = 1;
        _setupView.RowCount = 4;
        _setupView.Margin = new Padding(0);
        _setupView.BackColor = AppTheme.Background;
        _setupView.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
        _setupView.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        _setupView.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        _setupView.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _setupView.Controls.Add(BuildCaptureCard(), 0, 0);
        _setupView.Controls.Add(BuildBlacklistCard(), 0, 1);
        _setupView.Controls.Add(BuildBehaviorCard(), 0, 2);
        _setupView.Controls.Add(BuildOcrPreviewCard(), 0, 3);
    }

    private Control BuildCaptureCard()
    {
        var card = CreateCard(new Padding(15, 13, 15, 13), new Padding(0, 0, 0, 10));
        var layout = CreateTwoColumnLayout();

        var copy = CreateCopyBlock("Lobby-name area", _regionLabel);
        _regionLabel.AutoSize = true;
        _regionLabel.ForeColor = AppTheme.MutedText;
        _regionLabel.Margin = new Padding(0, 5, 0, 0);

        var actions = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0),
            BackColor = AppTheme.Surface
        };
        _selectRegion.Text = "Change area";
        _selectRegion.Size = new Size(112, 34);
        _selectRegion.Margin = new Padding(0, 0, 8, 0);
        AppTheme.StyleButton(_selectRegion, glyph: AppGlyph.SelectArea);
        _selectRegion.Click += SelectRegionClick;

        _testOcr.Text = "Test OCR";
        _testOcr.Size = new Size(96, 34);
        _testOcr.Margin = new Padding(0);
        AppTheme.StyleButton(_testOcr, glyph: AppGlyph.Ocr);
        _testOcr.Click += TestOcrClick;

        actions.Controls.AddRange([_selectRegion, _testOcr]);
        layout.Controls.Add(copy, 0, 0);
        layout.Controls.Add(actions, 1, 0);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildBlacklistCard()
    {
        var card = CreateCard(new Padding(15, 12, 15, 12), new Padding(0, 0, 0, 10));
        var layout = CreateTwoColumnLayout();

        _blacklistLabel.AutoSize = true;
        _blacklistLabel.ForeColor = AppTheme.MutedText;
        _blacklistLabel.Margin = new Padding(0, 5, 0, 0);
        var copy = CreateCopyBlock("Reviewed blacklist", _blacklistLabel);

        _updateBlacklist.Text = "Refresh";
        _updateBlacklist.Size = new Size(96, 34);
        _updateBlacklist.Anchor = AnchorStyles.Right;
        _updateBlacklist.Margin = new Padding(0);
        AppTheme.StyleButton(_updateBlacklist, glyph: AppGlyph.Refresh);
        _updateBlacklist.Click += async (_, _) => await RefreshBlacklistAsync();

        layout.Controls.Add(copy, 0, 0);
        layout.Controls.Add(_updateBlacklist, 1, 0);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildBehaviorCard()
    {
        var card = CreateCard(new Padding(15, 11, 15, 11), new Padding(0, 0, 0, 10));
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0),
            BackColor = AppTheme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var title = CreateSectionTitle("When a match is confirmed");
        title.Margin = new Padding(0, 0, 0, 7);
        layout.SetColumnSpan(title, 2);
        layout.Controls.Add(title, 0, 0);

        _warnOnly.Text = "Warn only\r\nYou leave the lobby manually";
        _warnOnly.Dock = DockStyle.Fill;
        _warnOnly.Margin = new Padding(0, 0, 5, 0);
        AppTheme.StyleModeOption(_warnOnly);
        _warnOnly.CheckedChanged += BehaviorModeChanged;

        _warnAndDodge.Text = "Warn and dodge\r\nCancel during the alert countdown";
        _warnAndDodge.Dock = DockStyle.Fill;
        _warnAndDodge.Margin = new Padding(5, 0, 0, 0);
        AppTheme.StyleModeOption(_warnAndDodge);
        _warnAndDodge.CheckedChanged += BehaviorModeChanged;

        _behaviorSequence.AutoSize = true;
        _behaviorSequence.ForeColor = AppTheme.MutedText;
        _behaviorSequence.Margin = new Padding(1, 8, 0, 0);
        layout.SetColumnSpan(_behaviorSequence, 2);

        layout.Controls.Add(_warnOnly, 0, 1);
        layout.Controls.Add(_warnAndDodge, 1, 1);
        layout.Controls.Add(_behaviorSequence, 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Control BuildOcrPreviewCard()
    {
        var card = CreateCard(new Padding(15, 10, 15, 10), new Padding(0));
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            BackColor = AppTheme.Surface
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = CreateSectionTitle("Last OCR result");
        title.Margin = new Padding(0, 0, 0, 6);

        _ocrPreview.Dock = DockStyle.Fill;
        _ocrPreview.Multiline = true;
        _ocrPreview.ReadOnly = true;
        _ocrPreview.ScrollBars = ScrollBars.Vertical;
        _ocrPreview.BackColor = AppTheme.Background;
        _ocrPreview.ForeColor = AppTheme.MutedText;
        _ocrPreview.BorderStyle = BorderStyle.FixedSingle;
        _ocrPreview.Text = "No OCR test has been run yet.";
        _ocrPreview.Margin = new Padding(0);

        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(_ocrPreview, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private void BuildMonitoringView()
    {
        _monitoringView.Dock = DockStyle.Fill;
        _monitoringView.Visible = false;
        _monitoringView.ColumnCount = 1;
        _monitoringView.RowCount = 5;
        _monitoringView.Margin = new Padding(0);
        _monitoringView.BackColor = AppTheme.Background;
        _monitoringView.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        _monitoringView.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _monitoringView.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _monitoringView.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _monitoringView.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var monitorMark = new PictureBox
        {
            Image = AppGlyphs.Create(AppGlyph.Ocr, AppTheme.Emerald, 42),
            SizeMode = PictureBoxSizeMode.CenterImage,
            Size = new Size(58, 58),
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 8)
        };
        var title = new Label
        {
            Text = "Watching the lobby-name area",
            AutoSize = true,
            Font = AppTheme.UiFont(14, FontStyle.Bold),
            ForeColor = AppTheme.Text,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 5)
        };
        var subtitle = new Label
        {
            Text = "Keep Dead by Daylight visible while players join.",
            AutoSize = true,
            ForeColor = AppTheme.MutedText,
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 16)
        };
        _monitoringLastScan.AutoSize = true;
        _monitoringLastScan.ForeColor = AppTheme.MutedText;
        _monitoringLastScan.Anchor = AnchorStyles.None;
        _monitoringLastScan.Text = "Waiting for the next scan…";

        _monitoringView.Controls.Add(monitorMark, 0, 1);
        _monitoringView.Controls.Add(title, 0, 2);
        _monitoringView.Controls.Add(subtitle, 0, 3);
        _monitoringView.Controls.Add(_monitoringLastScan, 0, 4);
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 15, 0, 0),
            BackColor = AppTheme.Background
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        footer.Controls.Add(new Label
        {
            Text = "Screen-only  •  nothing uploaded",
            AutoSize = true,
            ForeColor = AppTheme.MutedText,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0)
        }, 0, 0);

        _startStop.Text = "Start monitoring";
        _startStop.Font = AppTheme.UiFont(10, FontStyle.Bold);
        _startStop.Size = new Size(180, 38);
        _startStop.Margin = new Padding(0);
        AppTheme.StyleButton(_startStop, primary: true, glyph: AppGlyph.Play);
        _startStop.Click += StartStopClick;
        footer.Controls.Add(_startStop, 1, 0);
        return footer;
    }

    private static Panel CreateCard(Padding padding, Padding margin)
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Padding = padding,
            Margin = margin,
            BackColor = AppTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle
        };
    }

    private static TableLayoutPanel CreateTwoColumnLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            BackColor = AppTheme.Surface
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return layout;
    }

    private static TableLayoutPanel CreateCopyBlock(string title, Control detail)
    {
        var copy = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 2,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0),
            BackColor = AppTheme.Surface
        };
        copy.Controls.Add(CreateSectionTitle(title), 0, 0);
        copy.Controls.Add(detail, 0, 1);
        return copy;
    }

    private static Label CreateSectionTitle(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = AppTheme.UiFont(10, FontStyle.Bold),
            ForeColor = AppTheme.Text,
            Margin = new Padding(0)
        };
    }

    private void LoadSettingsIntoInterface()
    {
        _warnOnly.Checked = !_settings.AutoDodge;
        _warnAndDodge.Checked = _settings.AutoDodge;
        UpdateBehaviorModeAppearance();
        UpdateRegionLabel();
    }

    private void SaveSettingsFromInterface()
    {
        _settings.AutoDodge = _warnAndDodge.Checked;

        try
        {
            _settingsStore.Save(_settings);
        }
        catch (IOException exception)
        {
            SetStatus($"Could not save settings: {exception.Message}", StatusKind.Error);
        }
    }

    private void BehaviorModeChanged(object? sender, EventArgs e)
    {
        if (sender is RadioButton option && option.Checked)
        {
            UpdateBehaviorModeAppearance();
            SaveSettingsFromInterface();
        }
    }

    private void UpdateBehaviorModeAppearance()
    {
        AppTheme.RefreshModeOption(_warnOnly);
        AppTheme.RefreshModeOption(_warnAndDodge);
        _behaviorSequence.Text = _warnAndDodge.Checked
            ? "Alert shown  •  3-second countdown  •  Esc  •  Enter"
            : "Alert shown  •  you leave the lobby manually";
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
                "DBD Ranked cross-off lobby dodger",
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
            OcrScan scan = await _ocrService.RecognizeAsync(screenshot);
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
            _blacklistLabel.Text = $"●  {active} active entries  •  {source}  •  updated {result.Document.UpdatedAt:u}";

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
                "DBD Ranked cross-off lobby dodger",
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
        AppTheme.StyleButton(_startStop, glyph: AppGlyph.Stop);
        _setupView.Visible = false;
        _monitoringView.Visible = true;
        _monitoringView.BringToFront();
        SetStatus(
            _settings.AutoDodge
                ? "Monitoring. Confirmed matches will warn before the lobby is left."
                : "Monitoring in manual mode. Confirmed matches will warn only.",
            StatusKind.Monitoring);
    }

    private void StopMonitoring()
    {
        _monitorTimer.Stop();
        _monitoring = false;
        _candidateHits = 0;
        _lastCandidateKey = null;
        _startStop.Text = "Start monitoring";
        AppTheme.StyleButton(_startStop, primary: true, glyph: AppGlyph.Play);
        _monitoringView.Visible = false;
        _setupView.Visible = true;
        _setupView.BringToFront();
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
            OcrScan scan = await _ocrService.RecognizeAsync(screenshot);
            _ocrPreview.Text = string.IsNullOrWhiteSpace(scan.Text) ? "(No text recognized)" : scan.Text;
            int recognizedLines = scan.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Length;
            _monitoringLastScan.Text = $"Last scan: now  •  {recognizedLines} text line(s) recognized";
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

            if (_candidateHits < RequiredConsecutiveMatches)
            {
                SetStatus($"Possible match: {match.Alias}. Confirming on the next scan…", StatusKind.Warning);
                return;
            }

            await TriggerMatchAsync(match);
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

    private async Task TriggerMatchAsync(NameMatch match)
    {
        _cooldownUntil = DateTimeOffset.UtcNow.AddSeconds(
            Math.Clamp(_settings.MatchCooldownSeconds, 15, 300));
        _candidateHits = 0;
        _lastCandidateKey = null;

        SetStatus($"Blacklist match: {match.Alias} ({match.Entry.Group}).", StatusKind.Warning);
        var alert = new AlertForm(match, _settings.AutoDodge, countdownSeconds: 3);
        alert.FormClosed += (_, _) => alert.Dispose();
        alert.Show();

        if (!_settings.AutoDodge)
        {
            return;
        }

        AlertDecision decision = await alert.WaitForDecisionAsync();
        if (decision != AlertDecision.Dodge)
        {
            SetStatus($"Automatic dodge cancelled for {match.Alias}.", StatusKind.Warning);
            return;
        }

        LobbyDodgeResult result = await InputService.SendLobbyDodgeAsync();
        string actionText = result switch
        {
            LobbyDodgeResult.Success => "Lobby leave confirmed with Esc, then Enter.",
            LobbyDodgeResult.DeadByDaylightNotForeground => "Automatic dodge skipped: Dead by Daylight was not the foreground window.",
            LobbyDodgeResult.EscapeRejected => "Windows rejected the Escape input; dodge manually.",
            LobbyDodgeResult.FocusLostBeforeConfirmation => "Escape was sent, but Dead by Daylight lost focus before confirmation.",
            LobbyDodgeResult.EnterRejected => "The leave prompt opened, but Windows rejected the Enter input.",
            _ => "Automatic dodge did not complete; dodge manually."
        };
        alert.UpdateActionText(actionText);
        SetStatus(actionText, result == LobbyDodgeResult.Success ? StatusKind.Ready : StatusKind.Warning);
    }

    private async void BlacklistTimerTick(object? sender, EventArgs e)
    {
        await RefreshBlacklistAsync();
    }

    private void UpdateRegionLabel()
    {
        Rectangle region = _settings.CaptureRegion;
        _regionLabel.Text = _settings.HasCaptureRegion
            ? $"{region.Width} × {region.Height} px  •  X {region.X}, Y {region.Y}"
            : "No capture area selected";
        _selectRegion.Text = _settings.HasCaptureRegion ? "Change area" : "Select area";
    }

    private void SetStatus(string message, StatusKind kind)
    {
        _statusLabel.Text = message;
        _statusLabel.ForeColor = kind switch
        {
            StatusKind.Ready => AppTheme.MutedText,
            StatusKind.Monitoring => AppTheme.Emerald,
            StatusKind.Warning => AppTheme.Warning,
            StatusKind.Error => AppTheme.Danger,
            _ => AppTheme.MutedText
        };
        _statusPill.Text = kind switch
        {
            StatusKind.Ready => "●  Ready",
            StatusKind.Monitoring => "●  Monitoring",
            StatusKind.Warning => "●  Attention",
            StatusKind.Error => "●  Error",
            _ => "●  Ready"
        };
        _statusPill.ForeColor = _statusLabel.ForeColor;
        _statusPill.BackColor = kind switch
        {
            StatusKind.Warning => Color.FromArgb(58, 48, 25),
            StatusKind.Error => Color.FromArgb(64, 30, 32),
            _ => AppTheme.EmeraldSurface
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
