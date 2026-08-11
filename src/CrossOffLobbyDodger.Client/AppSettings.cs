using System.Drawing;

namespace CrossOff.LobbyDodger;

public sealed class AppSettings
{
    public const string DefaultBlacklistUrl =
        "https://raw.githubusercontent.com/kaankutluturk/crossoff-lobby-dodger/main/blacklist/blacklist.json";

    public int CaptureX { get; set; }
    public int CaptureY { get; set; }
    public int CaptureWidth { get; set; }
    public int CaptureHeight { get; set; }
    public bool AutoDodge { get; set; } = true;
    public string BlacklistUrl { get; set; } = DefaultBlacklistUrl;
    public int ScanIntervalMs { get; set; } = 1200;
    public int MatchCooldownSeconds { get; set; } = 45;

    public Rectangle CaptureRegion
    {
        get => new(CaptureX, CaptureY, CaptureWidth, CaptureHeight);
        set
        {
            CaptureX = value.X;
            CaptureY = value.Y;
            CaptureWidth = value.Width;
            CaptureHeight = value.Height;
        }
    }

    public bool HasCaptureRegion => CaptureWidth >= 20 && CaptureHeight >= 20;
}
