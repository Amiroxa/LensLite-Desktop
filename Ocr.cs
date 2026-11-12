
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using TesseractOCR;
using TesseractOCR.Enums;

namespace LensLite;

sealed class Ocr : IDisposable
{
    readonly string dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
    Engine? engine;
    string? engineLang;

    public string Recognize(Bitmap crop, string lang)
    {
        if (engine == null || engineLang != lang)
        {
            engine?.Dispose();
            engine = new Engine(dataPath, lang, EngineMode.LstmOnly);
            engineLang = lang;
        }

        using var prepared = Prepare(crop);
        using var ms = new MemoryStream();
        prepared.Save(ms, ImageFormat.Png);
        using var img = TesseractOCR.Pix.Image.LoadFromMemory(ms.ToArray());
        using var page = engine.Process(img, PageSegMode.Auto);
        return Clean(page.Text);
    }

    static Bitmap Prepare(Bitmap src)
    {
        int scale = src.Height < 60 ? 3 : src.Height < 140 ? 2 : 1;
        const int pad = 10;
        var dst = new Bitmap(
            src.Width * scale + 2 * pad,
            src.Height * scale + 2 * pad,
            PixelFormat.Format24bppRgb);

        using var g = Graphics.FromImage(dst);
        g.Clear(Color.White);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.DrawImage(src, pad, pad, src.Width * scale, src.Height * scale);
        return dst;
    }

    internal static string Clean(string raw)
    {
        var lines = raw.Replace("\r", "").Split('\n')
            .Select(l => l.Replace('ي', 'ی').Replace('ك', 'ک')
                .Replace("‏", "").Replace("‎", "").TrimEnd());

        return string.Join(Environment.NewLine, lines).Trim();
    }

    public void Dispose() => engine?.Dispose();
}
