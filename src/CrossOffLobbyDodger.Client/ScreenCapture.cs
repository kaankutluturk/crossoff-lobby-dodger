using System.Drawing;
using System.Drawing.Imaging;

namespace CrossOff.LobbyDodger;

public static class ScreenCapture
{
    public static Bitmap Capture(Rectangle region)
    {
        if (region.Width < 1 || region.Height < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(region), "Capture region must have positive dimensions.");
        }

        Rectangle virtualScreen = SystemInformation.VirtualScreen;
        if (!virtualScreen.IntersectsWith(region))
        {
            throw new ArgumentOutOfRangeException(nameof(region), "Capture region is outside the virtual desktop.");
        }

        var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format24bppRgb);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(region.Location, Point.Empty, region.Size, CopyPixelOperation.SourceCopy);
        return bitmap;
    }
}
