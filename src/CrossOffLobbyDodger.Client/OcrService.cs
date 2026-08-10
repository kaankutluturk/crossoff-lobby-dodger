using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using TesseractOCR;
using TesseractOCR.Enums;

namespace CrossOff.LobbyDodger;

public sealed class OcrService : IDisposable
{
    private readonly Engine _engine;

    public OcrService(string dataPath)
    {
        string model = Path.Combine(dataPath, "eng.traineddata");
        if (!File.Exists(model))
        {
            throw new FileNotFoundException(
                "OCR language data is missing. Extract the complete release ZIP before running CrossOff Lobby Dodger.",
                model);
        }

        _engine = new Engine(dataPath, Language.English, EngineMode.Default);
    }

    public Task<OcrScan> RecognizeAsync(Bitmap source, int threshold, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Recognize(source, threshold), cancellationToken);
    }

    private OcrScan Recognize(Bitmap source, int threshold)
    {
        using Bitmap prepared = PrepareForLobbyText(source, threshold);
        using var memory = new MemoryStream();
        prepared.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
        using var image = TesseractOCR.Pix.Image.LoadFromMemory(memory.ToArray());
        using var page = _engine.Process(image, PageSegMode.SparseText);
        return new OcrScan(page.Text?.Trim() ?? string.Empty, page.MeanConfidence);
    }

    private static Bitmap PrepareForLobbyText(Bitmap source, int threshold)
    {
        const int scale = 3;
        var scaled = new Bitmap(source.Width * scale, source.Height * scale, PixelFormat.Format24bppRgb);

        using (Graphics graphics = Graphics.FromImage(scaled))
        {
            graphics.Clear(Color.White);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(Point.Empty, scaled.Size));
        }

        Rectangle bounds = new(Point.Empty, scaled.Size);
        BitmapData data = scaled.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        try
        {
            unsafe
            {
                byte* firstPixel = (byte*)data.Scan0;
                for (int y = 0; y < data.Height; y++)
                {
                    byte* row = firstPixel + (y * data.Stride);
                    for (int x = 0; x < data.Width; x++)
                    {
                        byte* pixel = row + (x * 3);
                        int blue = pixel[0];
                        int green = pixel[1];
                        int red = pixel[2];
                        int luminance = ((red * 299) + (green * 587) + (blue * 114)) / 1000;
                        byte output = luminance >= threshold ? (byte)0 : (byte)255;
                        pixel[0] = output;
                        pixel[1] = output;
                        pixel[2] = output;
                    }
                }
            }
        }
        finally
        {
            scaled.UnlockBits(data);
        }

        return scaled;
    }

    public void Dispose() => _engine.Dispose();
}
