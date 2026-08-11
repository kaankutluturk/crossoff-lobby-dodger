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
                "OCR language data is missing. Extract the complete release ZIP before running the lobby dodger.",
                model);
        }

        _engine = new Engine(dataPath, Language.English, EngineMode.Default);
    }

    public Task<OcrScan> RecognizeAsync(Bitmap source, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Recognize(source), cancellationToken);
    }

    private OcrScan Recognize(Bitmap source)
    {
        using Bitmap prepared = PrepareForLobbyText(source);
        using var memory = new MemoryStream();
        prepared.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
        using var image = TesseractOCR.Pix.Image.LoadFromMemory(memory.ToArray());
        using var page = _engine.Process(image, PageSegMode.SparseText);
        return new OcrScan(page.Text?.Trim() ?? string.Empty, page.MeanConfidence);
    }

    private static Bitmap PrepareForLobbyText(Bitmap source)
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
                Span<int> histogram = stackalloc int[256];

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
                        histogram[luminance]++;
                    }
                }

                int threshold = Math.Clamp(FindOtsuThreshold(histogram, data.Width * data.Height), 100, 220);

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

    private static int FindOtsuThreshold(ReadOnlySpan<int> histogram, int totalPixels)
    {
        long weightedTotal = 0;
        for (int value = 0; value < histogram.Length; value++)
        {
            weightedTotal += (long)value * histogram[value];
        }

        long backgroundWeight = 0;
        long backgroundWeightedTotal = 0;
        double largestVariance = double.MinValue;
        int bestThreshold = 135;

        for (int threshold = 0; threshold < histogram.Length; threshold++)
        {
            backgroundWeight += histogram[threshold];
            if (backgroundWeight == 0)
            {
                continue;
            }

            long foregroundWeight = totalPixels - backgroundWeight;
            if (foregroundWeight == 0)
            {
                break;
            }

            backgroundWeightedTotal += (long)threshold * histogram[threshold];
            double backgroundMean = (double)backgroundWeightedTotal / backgroundWeight;
            double foregroundMean = (double)(weightedTotal - backgroundWeightedTotal) / foregroundWeight;
            double difference = backgroundMean - foregroundMean;
            double variance = backgroundWeight * (double)foregroundWeight * difference * difference;

            if (variance > largestVariance)
            {
                largestVariance = variance;
                bestThreshold = threshold;
            }
        }

        return bestThreshold;
    }

    public void Dispose() => _engine.Dispose();
}
