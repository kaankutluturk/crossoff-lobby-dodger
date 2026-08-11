using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CrossOff.LobbyDodger;

internal enum AppGlyph
{
    SelectArea,
    Ocr,
    Refresh,
    Play,
    Stop,
    Cancel,
    Warning
}

internal static class AppGlyphs
{
    public static Bitmap Create(AppGlyph glyph, Color color, int size = 18)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppPArgb);
        float scale = size / 18f;

        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.Clear(Color.Transparent);

        using var pen = new Pen(color, 1.55f * scale)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };
        using var brush = new SolidBrush(color);

        switch (glyph)
        {
            case AppGlyph.SelectArea:
                DrawSelectArea(graphics, pen, scale);
                break;
            case AppGlyph.Ocr:
                DrawOcr(graphics, pen, scale);
                break;
            case AppGlyph.Refresh:
                DrawRefresh(graphics, pen, brush, scale);
                break;
            case AppGlyph.Play:
                graphics.FillPolygon(brush,
                [
                    new PointF(6f * scale, 4f * scale),
                    new PointF(14f * scale, 9f * scale),
                    new PointF(6f * scale, 14f * scale)
                ]);
                break;
            case AppGlyph.Stop:
                graphics.FillRectangle(brush, 5.5f * scale, 5.5f * scale, 7f * scale, 7f * scale);
                break;
            case AppGlyph.Cancel:
                graphics.DrawLine(pen, 5f * scale, 5f * scale, 13f * scale, 13f * scale);
                graphics.DrawLine(pen, 13f * scale, 5f * scale, 5f * scale, 13f * scale);
                break;
            case AppGlyph.Warning:
                DrawWarning(graphics, pen, brush, scale);
                break;
        }

        return bitmap;
    }

    private static void DrawSelectArea(Graphics graphics, Pen pen, float scale)
    {
        graphics.DrawLines(pen,
        [
            new PointF(3f * scale, 7f * scale),
            new PointF(3f * scale, 3f * scale),
            new PointF(7f * scale, 3f * scale)
        ]);
        graphics.DrawLines(pen,
        [
            new PointF(11f * scale, 3f * scale),
            new PointF(15f * scale, 3f * scale),
            new PointF(15f * scale, 7f * scale)
        ]);
        graphics.DrawLines(pen,
        [
            new PointF(15f * scale, 11f * scale),
            new PointF(15f * scale, 15f * scale),
            new PointF(11f * scale, 15f * scale)
        ]);
        graphics.DrawLines(pen,
        [
            new PointF(7f * scale, 15f * scale),
            new PointF(3f * scale, 15f * scale),
            new PointF(3f * scale, 11f * scale)
        ]);
    }

    private static void DrawOcr(Graphics graphics, Pen pen, float scale)
    {
        DrawSelectArea(graphics, pen, scale);
        graphics.DrawLine(pen, 6f * scale, 7f * scale, 12f * scale, 7f * scale);
        graphics.DrawLine(pen, 6f * scale, 10f * scale, 12f * scale, 10f * scale);
        graphics.DrawLine(pen, 6f * scale, 13f * scale, 10f * scale, 13f * scale);
    }

    private static void DrawRefresh(Graphics graphics, Pen pen, Brush brush, float scale)
    {
        graphics.DrawArc(pen, 3.5f * scale, 3.5f * scale, 11f * scale, 11f * scale, -40, 285);
        graphics.FillPolygon(brush,
        [
            new PointF(13.8f * scale, 2.8f * scale),
            new PointF(14.6f * scale, 7.2f * scale),
            new PointF(10.7f * scale, 5.2f * scale)
        ]);
    }

    private static void DrawWarning(Graphics graphics, Pen pen, Brush brush, float scale)
    {
        graphics.DrawPolygon(pen,
        [
            new PointF(9f * scale, 2.8f * scale),
            new PointF(15.2f * scale, 14.3f * scale),
            new PointF(2.8f * scale, 14.3f * scale)
        ]);
        graphics.DrawLine(pen, 9f * scale, 6.3f * scale, 9f * scale, 10.2f * scale);
        graphics.FillEllipse(brush, 8.25f * scale, 11.6f * scale, 1.5f * scale, 1.5f * scale);
    }
}
